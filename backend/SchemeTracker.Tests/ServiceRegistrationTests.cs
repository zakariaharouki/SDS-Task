using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SchemeTracker.Api.Interfaces;
using SchemeTracker.Api.Models;
using SchemeTracker.Api.Repositories;
using SchemeTracker.Api.Services;

namespace SchemeTracker.Tests;

public class ServiceRegistrationTests
{
    // Settings are pinned here so these tests can never pick up a real connection string.
    private static WebApplicationFactory<Program> CreateFactory(string connectionString, string database) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
            host.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = connectionString,
                ["Mongo:Database"] = database
            })));

    [Fact]
    public void All_layers_resolve_from_the_container()
    {
        using var factory = CreateFactory("mongodb://localhost:27017", "schemetracker-tests");
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        Assert.IsType<SchemeService>(services.GetRequiredService<ISchemeService>());
        Assert.IsType<SchemeRepository>(services.GetRequiredService<ISchemeRepository>());
        Assert.Equal("schemes", services.GetRequiredService<IMongoCollection<Scheme>>().CollectionNamespace.CollectionName);
        Assert.Equal("schemetracker-tests", services.GetRequiredService<IMongoCollection<Scheme>>().Database.DatabaseNamespace.DatabaseName);
    }

    [Fact]
    public void Startup_fails_when_the_connection_string_is_missing()
    {
        using var factory = CreateFactory(string.Empty, "schemetracker-tests");

        Assert.Throws<OptionsValidationException>(() => factory.Services);
    }
}
