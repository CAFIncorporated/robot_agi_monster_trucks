# Coordinate Service

A .NET 10 microservice for coordinate systems and points: create systems (width×height), add a point per system with position and facing direction (N/S/E/W), and move points using commands (M=move forward, R=turn right, L=turn left). Uses PostgreSQL as the source of truth and an in-memory response cache.

See [docs/architecture.md](docs/architecture.md) for a Mermaid flow of how the service is called from a gateway, how caching and cache eviction work, and how PostgreSQL is used.

## Project Structure
```
**.github/** — CI/CD workflows (ci, cd), repo settings
**config/** — appsettings per environment
**deploy/** — Helm chart (templates, values per env)
**docker/** — docker-compose, prod/test Dockerfiles
**lib/CoordinateService/** — app (Controllers, Models, Services, Program)
**test/** — unit and DB persistence tests
**clients/** — OpenAPI-generated C# client
**clients_test/** — client/integration tests
**scripts/** — E2E curl script
**openapi.json**, **CoordinateService.sln**, **docker-bake.hcl**, **Makefile**
```

## Quick Start

### Local development with Docker Compose

```bash
make up        # Start app + PostgreSQL (port 18080)
make logs     # Tail logs
make down     # Stop everything
```

### Build images

```bash
make bake          # Build all images (prod + test)
make bake-prod     # Build production image only
make bake-test     # Build test image only
```

### Run tests

```bash
make test                  # All tests locally (requires .NET SDK; DB tests use Testcontainers)
make test-db-writes        # DB persistence tests only (Testcontainers; container removed after)
make test-db-writes-compose # DB persistence tests vs compose stack; data stays in psql for inspection (run make up first)
make docker-test           # Build test image and run tests in Docker
make e2e                   # Start compose and run curl E2E script
```

Database persistence tests call the API then query PostgreSQL to verify writes. Use test-db-writes-compose if you want to inspect data in the compose psql container after the run.

### OpenAPI spec

```bash
make up
make generate-spec   # Fetches swagger from app and writes openapi.json
```


## API Endpoints

Base URL when using Docker Compose: http://localhost:18080. The API is versioned under /api/v1/.

### Health

| Method | Path      | Description                    |
|--------|-----------|--------------------------------|
| GET  | /healthz | Liveness probe                 |
| GET  | /readyz  | Readiness (includes Postgres)  |

### Coordinate systems

| Method   | Path                              | Description                    |
|----------|-----------------------------------|--------------------------------|
| POST   | /api/v1/coordinate-systems      | Create system (returns id)   |
| GET    | /api/v1/coordinate-systems/{id} | Get system (includes point)    |
| DELETE | /api/v1/coordinate-systems/{id} | Delete system (204 No Content) |

### Points

| Method   | Path                                          | Description                          |
|----------|-----------------------------------------------|--------------------------------------|
| POST   | /api/v1/coordinate-systems/{systemId}/points | Create point in system (one per system) |
| GET    | /api/v1/points/{pointId}                    | Get point by ID                      |
| POST   | /api/v1/points/{pointId}/move               | Move by commands (see below)         |
| DELETE | /api/v1/points/{pointId}                    | Delete point (204 No Content)        |

### Move commands

POST /api/v1/points/{pointId}/move body: { "commands": ["M", "R", "L", ...] }

**M** — Move one step in current direction (N=Y−1, S=Y+1, E=X+1, W=X−1).
**R** — Turn right (e.g. E→S, S→W, W→N, N→E).
**L** — Turn left (e.g. E→N, N→W, W→S, S→E).

Response: updated position and direction (same shape as point). 400 if any move goes out of bounds or commands are invalid.

### Request IDs

Send X-Request-Id on any request; the response echoes it back.

### Example requests
```bash
BASE="http://localhost:18080"
```

# Health
```bash
curl -s "$BASE/healthz"
curl -s "$BASE/readyz"

# Create coordinate system
curl -s -X POST "$BASE/api/v1/coordinate-systems" \
  -H "Content-Type: application/json" \
  -d '{"name":"grid","width":10,"height":10}'
# → {"id":"<uuid>"}

# Get system (use id from above)
curl -s "$BASE/api/v1/coordinate-systems/<system-id>"

# Create point in system (x, y, direction N|S|E|W)
curl -s -X POST "$BASE/api/v1/coordinate-systems/<system-id>/points" \
  -H "Content-Type: application/json" \
  -d '{"x":5,"y":5,"direction":"N"}'
# → {"id":"<uuid>","systemId":"...","x":5,"y":5,"direction":"N"}

# Get point
curl -s "$BASE/api/v1/points/<point-id>"

# Move point (M=forward, R=right, L=left)
curl -s -X POST "$BASE/api/v1/points/<point-id>/move" \
  -H "Content-Type: application/json" \
  -d '{"commands":["R","M","M","L"]}'
# → {"id":"...","x":7,"y":5,"direction":"N","systemId":"..."}

# Delete point
curl -s -X DELETE "$BASE/api/v1/points/<point-id>"

# Delete system
curl -s -X DELETE "$BASE/api/v1/coordinate-systems/<system-id>"
```
## Client

The C# client in clients/CoordinateService.Client is generated from openapi.json (NSwag). Register it with IHttpClientFactory:
csharp
services.AddCoordinateServiceClient("https://api.example.com/");
// Then inject CoordinateServiceClient

See clients/CoordinateService.Client/README.md for regeneration and options.

## Architecture
Client → REST API → In-memory cache (optional) → PostgreSQL

- Writes and cache invalidation go to PostgreSQL; reads use cache when present.
- Health: /healthz (liveness), /readyz (readiness with Postgres check).

## Configuration

Environment-specific config is loaded via ASPNETCORE_ENVIRONMENT and corresponding appsettings.{env}.json.

ConnectionStrings__Postgres — PostgreSQL connection string
