using Asp.Versioning;
using CoordinateService.Middleware;
using CoordinateService.Services;
using Sanitization;
using Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Coordinate Service",
        Version = "v1",
        Description = "API for managing coordinate systems and points"
    });
});

builder.Services.AddSingleton<ICoordinateStore, PostgresCoordinateStore>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

builder.Services.AddPassthroughSanitization();
builder.Services.AddConsoleMetrics();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var store = scope.ServiceProvider.GetRequiredService<ICoordinateStore>();
    await store.InitializeAsync();
}

app.UseRequestId();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Coordinate Service v1"));

app.MapGet("/healthz", () => Results.Ok("Healthy")).ExcludeFromDescription();

app.MapGet("/readyz", async (ICoordinateStore store) =>
{
    var healthy = await store.IsHealthyAsync();
    return healthy ? Results.Ok("Ready") : Results.StatusCode(503);
}).ExcludeFromDescription();

app.MapControllers();

app.Run();

public partial class Program { }
