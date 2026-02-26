# NATS-based Coordinate Service — Architectural impact

This doc describes, at an **architectural level only**, what would need to change to move from HTTP to NATS (produce/consume) and how that affects routing to pods, crash recovery, and client generation.

---

## 1. Components that would need to change

| Component | Today (HTTP) | With NATS |
|-----------|--------------|-----------|
| **Coordinate Service pods** | ASP.NET app: HTTP API, controllers, middleware, `ICoordinateStore`, `ICacheService`. | Same core (store, cache, DB) but **entry point** becomes **NATS consumers** (subscribe to subjects, deserialise messages, call existing application logic, publish replies or outcome). Optionally keep an HTTP API for health/read-only or migration. |
| **Gateway / callers** | Use `HttpClient` (or generated HTTP client) to send requests to the service. | **Produce** messages to NATS subjects (e.g. `coordinate.create.system`, `coordinate.point.move`) and either **subscribe** to reply subjects or use **request–reply** over NATS. No HTTP call to the service. |
| **Contract / API surface** | OpenAPI (HTTP paths, methods, request/response bodies). | **Subject naming** and **message schemas** (e.g. JSON payloads per subject). No URLs; operations map to subjects and message types. |
| **Routing / load balancing** | K8s Service + optional gateway consistent hashing. | Determined by **who subscribes to which subjects** and **queue groups** (see below). No HTTP load balancer in front of the service for command traffic. |
| **Client library** | Generated HTTP client (NSwag from OpenAPI): methods like `CreateCoordinateSystemAsync`, `MovePointAsync`. | **Different artefact:** producer/consumer helpers or a small SDK that **publishes** to the right subjects and **subscribes** (or does request–reply) for responses. Not generated from OpenAPI; see “Client generation” below. |

So at a high level: **service** gains NATS consumers that delegate to existing logic; **callers** become producers (and possibly consumers of replies); **contract** is subject + message schema; **client generation** changes because the interface is no longer HTTP.

---

## 2. Routing traffic to the correct pods (same system/point → same pod)

With NATS, “routing” means **which consumer (which pod) gets which message**.

- **Subject design** — Use the resource id in the subject so NATS (or JetStream) can deliver consistently:
  - Option A: **Subject per resource**, e.g. `coordinate.system.{systemId}.commands` and `coordinate.point.{pointId}.commands`. Subscribers subscribe to a wildcard (e.g. `coordinate.point.>`) but **only one consumer group** per subject pattern, so each subject is consumed by one subscriber. That requires **dynamic subscriptions** per system/point or a fixed set of partitions (see below).
  - Option B: **Single command subject + message key** — e.g. one subject `coordinate.commands` with a **header or property** `systemId` / `pointId`. NATS Core does not partition by key; **JetStream** allows **subject mapping** or you use a **dispatcher** that subscribes to `coordinate.commands`, hashes the key, and forwards to internal queues (one queue per resource or per bucket of resources). Only the dispatcher’s subscribers need to agree on who handles which key (e.g. consistent hash).
  - Option C: **Sharded subjects** — e.g. `coordinate.commands.{hash(systemId) % N}` with N fixed (e.g. number of pods). All commands for the same system go to the same subject; each pod subscribes to one or more of these subjects (static assignment). So “same system” → same subject → same pod(s) subscribed to that subject.

- **Queue groups** — Use a **queue group** so that for a given subject, **only one** of the Coordinate Service instances gets each message. That gives load balancing and at-most-once delivery per message. To get “same resource → same pod” you still need the **subject** (or an internal router) to encode the resource id (e.g. Option A or C above).

- **Summary** — To ensure “traffic for system/point X goes to the correct pod”: either (1) put resource id in the subject and have a fixed mapping from subject to pod (e.g. sharded subjects so each pod subscribes to a subset), or (2) use a single subject and a **single dispatcher** that hashes the key and forwards to per-resource or per-shard queues consumed by different pods. The service pods then consume from those queues; no HTTP routing involved.

---

## 3. No pods have the consumers / pod responsible for a system crashes

**Problem:** If the pod that was responsible for a given system (or set of points) crashes, who processes new messages for that system?

- **Queue groups** — With a **queue group**, another pod in the group can take over: NATS redelivers to the next available subscriber. So if you use one subject (or a small set of subjects) and a queue group, **any live pod** can process the message. That gives **high availability** but **no guarantee** that the same pod always handles the same system (so in-memory cache per pod is less useful unless you re-subscribe by resource).

- **Durable JetStream consumers** — With **JetStream**, use **durable consumers** and **persistent streams**. If a pod dies, messages are not lost; another pod (or the same pod after restart) can consume from the same durable consumer and continue. So “starting” is just: **pods start and attach to the same durable consumer**; no extra “start the consumer” step. If you want “same resource → same pod,” you still need subject design (e.g. sharded subjects) so that when a pod restarts, it re-subscribes to the same shard(s) and effectively “takes back” that slice of the key space.

- **Reassignment / rebalancing** — If each pod is assigned a subset of resources (e.g. by hash), when a pod crashes you need either (1) **rebalance**: remaining pods (or a controller) recompute assignments and (re)subscribe to the subjects for the orphaned resources, or (2) **let any pod handle any message** (single queue group) and accept that cache locality is best-effort. So “how do we start them” — **pods start on deploy/restart and subscribe to their assigned subjects or join the queue group**; no separate “start consumer” RPC. A **controller** (or K8s operator) could watch pod lifecycle and update subject subscriptions or JetStream consumer config if you need explicit reassignment.

- **Summary** — With NATS (and JetStream): use **durable consumers and streams** so messages survive pod crashes; **queue groups** so another pod can process when one dies. “Starting” consumers = pods come up and subscribe (or reattach to durable consumers). For strict “same resource → same pod” after crash, you need a **rebalance** so that some pod takes over the dead pod’s subject shards or keys.

---

## 4. Client generation: how it would change

Today the client is **generated from OpenAPI** and used to **send HTTP requests** to the Coordinate Service. With NATS, callers **produce and consume messages**; there is no HTTP API for those operations, so OpenAPI/NSwag is no longer the right source of truth for the “command” surface.

- **Contract** — The contract becomes **subjects + message schemas** (e.g. “publish to `coordinate.point.move` with payload `{ pointId, commands }`; expect reply on `coordinate.replies.{replyId}` or inline request–reply”). That is **async/messaging** rather than REST, so **OpenAPI** (HTTP-centric) does not describe it well. **AsyncAPI** is the usual spec for event-driven APIs (channels = subjects, operations, message payloads).

- **What gets “generated”** — Instead of an HTTP client (e.g. NSwag):
  - **Option A:** **Generate from AsyncAPI** — Use an AsyncAPI spec (subjects, message schemas) and a codegen tool (e.g. AsyncAPI generators for .NET) to produce **producer** and **consumer** code (publish/subscribe, DTOs). The “client” for the gateway would be a **producer** that publishes to the right subjects and (if needed) subscribes to reply subjects or uses request–reply.
  - **Option B:** **Hand-written or thin SDK** — A small library that wraps NATS (e.g. `ICoordinateServiceProducer`: `PublishCreateSystemAsync`, `PublishMovePointAsync`) and uses shared DTOs. No codegen from a spec unless you introduce AsyncAPI.
  - **Option C:** **OpenAPI for read-only HTTP + AsyncAPI for commands** — Keep HTTP for queries (e.g. GET point) and health; use NATS only for commands. Then you still have an HTTP client (from OpenAPI) for reads and a separate producer (from AsyncAPI or hand-written) for commands.

- **Testing** — Today, client tests use the **HTTP client** against the service. With NATS, tests would **publish** to NATS and either (1) **subscribe** to reply/inbox to assert on outcomes, or (2) **query the service** (e.g. HTTP GET if kept) or the DB to verify. So the test harness becomes “producer + consumer or HTTP for verification” instead of “HTTP client only.”

**Summary:** Implementing NATS means the **client** is no longer “HTTP client generated from OpenAPI.” It becomes a **producer (and possibly consumer)** for NATS subjects; contract is **AsyncAPI** (or equivalent); generation, if any, is from AsyncAPI or a thin hand-written producer/SDK. OpenAPI/NSwag remains relevant only for any **HTTP surface** you keep (e.g. health, read-only GETs).
