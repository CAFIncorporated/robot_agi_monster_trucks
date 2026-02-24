namespace CoordinateService.Middleware;

public class RequestIdMiddleware
{
    private readonly RequestDelegate _next;
    public const string HeaderName = "X-Request-Id";

    public RequestIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers[HeaderName].FirstOrDefault()
                        ?? Guid.NewGuid().ToString("N");

        context.Items["RequestId"] = requestId;
        context.Response.Headers[HeaderName] = requestId;

        await _next(context);
    }
}

public static class RequestIdMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestId(this IApplicationBuilder builder) =>
        builder.UseMiddleware<RequestIdMiddleware>();
}
