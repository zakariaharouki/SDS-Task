using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchemeTracker.Api.Interfaces;

namespace SchemeTracker.Tests.Support;

/// <summary>Hosts the real API in memory, with the repository replaced by a fake.</summary>
public class SchemeApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mongo:ConnectionString"] = "mongodb://localhost:27017",
            ["Mongo:Database"] = "schemetracker-tests"
        }));

        builder.ConfigureTestServices(services =>
            services.AddSingleton<ISchemeRepository, FakeSchemeRepository>());
    }
}
