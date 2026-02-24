using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Sanitization;

public static class SanitizationServiceExtensions
{
    public static IServiceCollection AddPassthroughSanitization(this IServiceCollection services)
    {
        services.AddSingleton(typeof(ISanitizer<>), typeof(PassthroughSanitizer<>));

        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add<SanitizationActionFilter>();
        });

        return services;
    }
}
