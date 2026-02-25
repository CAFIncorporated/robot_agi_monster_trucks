# State Management

Where should the service keep state: in-memory only, with a shared cache (e.g. Redis), or with a database as source of truth?

## Options

**A. In-memory only** — Store coordinate systems and points in process memory.

**B. Redis (or shared cache) + optional DB** — Use Redis for caching and possibly as shared state; optional PostgreSQL for durability.

**C. PostgreSQL as source of truth** — Use PostgreSQL for persistent storage; optional in-process cache for reads.

## Pros and cons

| Option | Pros | Cons |
|--------|------|------|
| **A. In-memory** | No external dependencies; very fast. | Data lost on restart; no sharing across pods; early data can be evicted under load. |
| **B. Redis** | Good when multiple pods need the same cached data; can reduce DB load. | Extra component (Redis sidecar or cluster); still need a durable store if data must survive restarts. |
| **C. PostgreSQL** | Durable source of truth; works across restarts and replicas; no guarantee that “everything stays in memory.” | Requires a DB (e.g. shared PostgreSQL in the namespace); slightly more ops and latency than pure memory. |

## Decision

**PostgreSQL as source of truth**, with an in-process cache for reads. The service cannot rely on in-memory state alone (no guarantee data stays in memory across requests or restarts). A shared cache (e.g. Redis) is not required for the current use case. PostgreSQL provides long-term storage; the app uses it for all writes and cache invalidation, and caches reads where useful.
