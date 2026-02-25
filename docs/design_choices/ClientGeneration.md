# Client Generation

How should downstream services (e.g. gateway, other pods) call the Coordinate Service API — hand-written HTTP, or a typed client generated from the OpenAPI spec? If generated, which tool?

## Options

**A. Hand-written client** — Manually implement a small client that uses `HttpClient`, builds URLs, and (de)serializes DTOs.

**B. NSwag** — Generate a C# client and DTOs from `openapi.json`; MSBuild runs NSwag on build; output is a single class (e.g. `CoordinateServiceClient`) with async methods per operation.

**C. OpenAPI Generator** — Language-agnostic codegen from OpenAPI; many targets (C#, Java, Go, TypeScript, etc.); good when multiple languages need clients from the same spec.

**D. Kiota (Microsoft)** — Generates clients from OpenAPI for several languages; path-based API model; can produce smaller, more focused clients.

**E. Refit** — You define C# interfaces; Refit generates HTTP implementations at runtime. OpenAPI can drive interface shape via extra tooling.

## Pros and cons

| Option | Pros | Cons |
|--------|------|------|
| **A. Hand-written** | Full control; no codegen; no tool quirks. | Must keep client in sync with API by hand; more boilerplate and risk of drift. |
| **B. NSwag** | .NET-native; good MSBuild integration; single generated file; works with `IHttpClientFactory`. | Known issue: nullable/optional types can be generated or annotated inconsistently (see below). |
| **C. OpenAPI Generator** | One spec, many languages; widely used. | Heavier; C# target is one of many; less tailored to .NET than NSwag. |
| **D. Kiota** | Modern; multi-language; can generate lean clients. | Different API style; newer ecosystem; may require more customisation. |
| **E. Refit** | Interface-based; testable; minimal generated surface. | OpenAPI→Refit typically needs another step or hand-written interfaces. |

## Decision

**NSwag.** We want a typed C# client generated from the OpenAPI spec so that (1) other services use the same contract as the API, (2) integration tests call the service the same way the gateway will, and (3) contract drift is caught when we regenerate and run tests. NSwag fits our .NET build, `IHttpClientFactory`, and single-language use case. OpenAPI Generator and Kiota are better when multiple languages need clients; Refit and hand-written are viable but add more ongoing sync work.

**How it’s used:** Other services register the client (e.g. `AddCoordinateServiceClient(baseUrl)`) and inject `CoordinateServiceClient`; they call methods like `CreateCoordinateSystemAsync`, `GetPointAsync`, and handle non–2xx via `SwaggerException`. Client tests use the same client against a running app or `WebApplicationFactory`, so testing mirrors real usage.

**Known issue (NSwag):** Nullable and optional types can be generated or annotated inconsistently (e.g. optional properties not marked nullable, or wrong `Required`/`DisallowNull` attributes), which can cause noisy types or deserialization issues. We use `generateNullableReferenceTypes: false`; it still generates nullable types. See [NSwag#4313](https://github.com/RicoSuter/NSwag/issues/4313).
