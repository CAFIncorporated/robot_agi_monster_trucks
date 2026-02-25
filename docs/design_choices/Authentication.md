# API Access and Authentication

How should the API be exposed and who can call it? Should we add user or application authentication?

## Options

**A. Internal-only, no auth** — API is reachable only by other services in the cluster (e.g. via gateway); no authentication.

**B. Internal + network policy** — Same as A, with Kubernetes NetworkPolicy so only named services (e.g. from `values.yaml`) can reach the API.

**C. External or multi-tenant + auth** — API may be called by external clients or multiple teams; add token-based or integration with a central auth service; DB and API enforce auth.

## Pros and cons

| Option | Pros | Cons |
|--------|------|------|
| **A. Internal, no auth** | Simple; no tokens or user management; fits single-tenant, in-cluster callers. | No protection if network is compromised or if API is exposed by mistake. |
| **B. Internal + netpol** | Limits which pods can talk to the API; easy to maintain (service names in values). | Still no authentication; only restricts by pod identity. |
| **C. Auth** | Enables row-level/table security and audit; supports multi-tenant or external access. | More complexity (tokens, user/app identity, DB permissions); may be unnecessary for internal-only use. |

## Decision

**Internal-only, no authentication.** The API is intended to be called by a gateway or other services inside the cluster; access and data handling are not tied to end users or applications (for now). If requirements change (external access or multi-team use), we can add token-based auth or integrate with an existing auth service and tighten DB permissions later.
