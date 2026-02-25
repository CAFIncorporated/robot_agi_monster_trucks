TODO: check the business case for this. consider diff scenarios where each solution may be best.
persistent connection where commands come 1 after another in short span would need websocket, but if commands are more sporadic and not time-sensitive, HTTP may be sufficient. also consider the complexity of implementing and maintaining a websocket connection compared to HTTP, as well as the scalability implications of each approach.
# API Connection: WebSocket vs HTTP

How should clients talk to the API — persistent WebSocket or request-response HTTP?

Resources: https://medium.com/@codebob75/creating-and-consuming-apis-in-net-c-d24f9c414b96

## Options

**A. HTTP only** — REST over HTTP; one request, one response; no long-lived connection.

**B. WebSocket** — Persistent, full-duplex connection; server can push updates; low latency for many small messages.

**C. Both** — API supports HTTP and WebSocket so different clients can choose.

## Pros and cons

| Option | Pros | Cons |
|--------|------|------|
| **A. HTTP** | Simple; stateless; easy to scale and integrate (curl, gateways, load balancers). | New connection per request; no server push; polling if client needs updates. |
| **B. WebSocket** | Real-time, full-duplex; lower latency when commands are frequent and sequential. | More complex (connection lifecycle, scaling, timeouts); not all infra supports it equally. |
| **C. Both** | Flexibility for different clients and use cases. | Two code paths to implement and maintain; higher complexity. |

## Decision

**HTTP.** Commands are sporadic and not highly time-sensitive; request-response is enough. HTTP is simpler to implement, operate, and integrate with gateways and existing tooling (e.g. e2e with curl). If we later need real-time streaming or very frequent commands in a short burst, we can consider adding a WebSocket endpoint.
