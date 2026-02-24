using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Observability;

public static class ObservabilityServiceExtensions
{
    public static IServiceCollection AddConsoleMetrics(this IServiceCollection services)
    {
        services.AddSingleton<IMetricsRecorder, ConsoleMetricsRecorder>();

        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add<MetricsActionFilter>();
        });

        return services;
    }
}
