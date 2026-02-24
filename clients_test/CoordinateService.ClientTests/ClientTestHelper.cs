using System.Net;
using CoordinateService.Client;

namespace CoordinateService.ClientTests;

internal static class ClientTestHelper
{
    public static async Task<(T? Data, HttpStatusCode StatusCode)> RunAsync<T>(Func<Task<T>> call)
    {
        try
        {
            var data = await call();
            return (data, HttpStatusCode.OK);
        }
        catch (SwaggerException ex)
        {
            return (default, ex.StatusCode);
        }
    }

    public static async Task<HttpStatusCode> RunAsync(Func<Task> call)
    {
        try
        {
            await call();
            return HttpStatusCode.OK;
        }
        catch (SwaggerException ex)
        {
            return ex.StatusCode;
        }
    }

    public static async Task<HttpStatusCode> RunAsync(Func<Task<HttpStatusCode>> call)
    {
        try
        {
            return await call();
        }
        catch (SwaggerException ex)
        {
            return ex.StatusCode;
        }
    }
}
