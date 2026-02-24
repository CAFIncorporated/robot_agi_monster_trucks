using Microsoft.Extensions.DependencyInjection;

namespace CoordinateService.Client;

/// <summary>Registers <see cref="CoordinateServiceClient"/> as a typed HttpClient with <see cref="IHttpClientFactory"/>.</summary>
public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddCoordinateServiceClient(this IServiceCollection services, string baseAddress)
    {
        return services.AddHttpClient<CoordinateServiceClient>(client =>
        {
            client.BaseAddress = new Uri(baseAddress.TrimEnd('/') + "/");
        });
    }

    public static IHttpClientBuilder AddCoordinateServiceClient(this IServiceCollection services, Action<HttpClient>? configureClient = null)
    {
        var builder = services.AddHttpClient<CoordinateServiceClient>();
        if (configureClient != null)
            builder.ConfigureHttpClient(configureClient);
        return builder;
    }
}
