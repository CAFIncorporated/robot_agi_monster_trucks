# Coordinate Service – Architecture

The service is intended to be called from a **gateway service** inside the cluster. Clients do not call the Coordinate Service directly; the gateway routes requests and may handle auth, rate limiting, and aggregation.

## Request flow (gateway → service → cache → PostgreSQL)

```mermaid
flowchart LR
    subgraph cluster["Kubernetes cluster"]
        GW[Gateway Service]
        subgraph coord["Coordinate Service"]
            API[API layer]
            CACHE[(In-memory cache)]
            API --> CACHE
            CACHE --> DB
        end
        GW -->|"HTTP /api/v{version}/..."| API
    end
    DB[(PostgreSQL)]
```
## Cache and database behaviour

**Read path:** API checks the cache first (by key, e.g. point:{id} or system:{id}). On cache miss, the service reads from PostgreSQL, stores the result in the cache, then returns the response.
**Write path (create/update/delete):** The service always writes to PostgreSQL first, then **evicts** the relevant cache entries so the next read sees fresh data from the database.

```mermaid
flowchart TB
    subgraph read["Read (e.g. GET point)"]
        R1[Request] --> R2{Cache hit?}
        R2 -->|Yes| R3[Return cached]
        R2 -->|No| R4[Read from PostgreSQL]
        R4 --> R5[Put in cache]
        R5 --> R6[Return]
    end

    subgraph write["Write (create / update / delete)"]
        W1[Request] --> W2[Write to PostgreSQL]
        W2 --> W3[Evict cache for affected keys]
        W3 --> W4[Return]
    end
```

## How PostgreSQL is used

| Operation | PostgreSQL usage | Cache action |
|-----------|------------------|--------------|
| Create system | INSERT into coordinate_systems | None |
| Get system | SELECT from coordinate_systems | Set on miss |
| Delete system | DELETE + cascade | Evict system + all points |
| Create point | INSERT into points | Evict system |
| Get point | SELECT from points | Set on miss |
| Move point | UPDATE on points | Evict point + system |
| Delete point | DELETE from points | Evict point + system |

The service holds a single connection string (e.g. from ConnectionStrings:Postgres or env) and uses it for all operations. Schema is created on startup via InitializeAsync() (e.g. CREATE TABLE IF NOT EXISTS ...).
