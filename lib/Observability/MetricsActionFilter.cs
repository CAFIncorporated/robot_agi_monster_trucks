using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Observability;

/// <summary>
/// Global action filter that times every controller action automatically.
/// Logs as "Controller.Action" so the source is always identifiable.
/// </summary>
public class MetricsActionFilter : IAsyncActionFilter
{
    private readonly IMetricsRecorder _metrics;

    public MetricsActionFilter(IMetricsRecorder metrics)
    {
        _metrics = metrics;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var operationName = context.ActionDescriptor is ControllerActionDescriptor descriptor
            ? $"{descriptor.ControllerName}.{descriptor.ActionName}"
            : context.ActionDescriptor.DisplayName ?? "Unknown";

        var sw = Stopwatch.StartNew();
        try
        {
            await next();
        }
        finally
        {
            sw.Stop();
            _metrics.Record(operationName, sw.Elapsed);
        }
    }
}
