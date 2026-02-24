using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using CoordinateService.Services;

namespace CoordinateService.ClientTests;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var desc = services.SingleOrDefault(d => d.ServiceType == typeof(ICoordinateStore));
            if (desc != null) services.Remove(desc);
            services.AddSingleton<ICoordinateStore, InMemoryCoordinateStore>();
        });

        builder.UseEnvironment("Test");
    }
}
