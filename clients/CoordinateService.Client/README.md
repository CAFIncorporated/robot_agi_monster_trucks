# Coordinate Service API Client

This client is **generated from the OpenAPI spec** (`openapi.json` at repo root) and is the source of truth for the API contract.

## Regenerating the client

1. Ensure the API spec is up to date: run `make generate-spec` (with the service running) to refresh `openapi.json`.
2. Build the client project; the build copies the spec and runs NSwag to regenerate `Generated/CoordinateServiceClient.g.cs`:

   ```bash
   dotnet build clients/CoordinateService.Client/CoordinateService.Client.csproj
   ```

   Or run NSwag manually from the client directory:

   ```bash
   cd clients/CoordinateService.Client
   dotnet tool run nswag run nswag.json   # or use the NSwag.MSBuild target
   ```

## Using the client with IHttpClientFactory

Register the typed client so the factory provides `HttpClient` to `CoordinateServiceClient`:

```csharp
// In Program.cs or Startup
services.AddCoordinateServiceClient("https://api.example.com/");

// Inject and use
public class MyService
{
    private readonly CoordinateServiceClient _client;
    public MyService(CoordinateServiceClient client) => _client = client;

    public async Task DoWorkAsync()
    {
        var sys = await _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "grid", Width = 10, Height = 10 });
    }
}
```

For non-2xx responses, the client throws `SwaggerException` with `StatusCode` and `Response` set.
