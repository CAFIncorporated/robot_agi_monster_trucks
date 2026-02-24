using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Sanitization;

/// <summary>
/// Global action filter that sanitizes every action argument before the action executes.
/// Resolves ISanitizer&lt;T&gt; for each argument's runtime type from DI.
/// </summary>
public class SanitizationActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;

        foreach (var key in context.ActionArguments.Keys)
        {
            var value = context.ActionArguments[key];
            if (value is null) continue;

            var valueType = value.GetType();
            var sanitizerType = typeof(ISanitizer<>).MakeGenericType(valueType);
            var sanitizer = services.GetService(sanitizerType) as ISanitizer;

            if (sanitizer is not null)
            {
                context.ActionArguments[key] = sanitizer.Sanitize(value);
            }
        }

        await next();
    }
}
