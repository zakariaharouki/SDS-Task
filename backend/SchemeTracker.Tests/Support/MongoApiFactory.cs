using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SchemeTracker.Tests.Support;

/// <summary>
/// Hosts the whole API against the MongoDB test container, including the real repository and the
/// index setup at startup. Each instance gets its own database, so tests can't affect each other.
/// </summary>
public sealed class MongoApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly TimeProvider? _clock;

    public MongoApiFactory(string connectionString, TimeProvider? clock = null)
    {
        _connectionString = connectionString;
        _clock = clock;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mongo:ConnectionString"] = _connectionString,
            ["Mongo:Database"] = "test-" + Guid.NewGuid().ToString("N")
        }));

        if (_clock is not null)
        {
            builder.ConfigureTestServices(services => services.AddSingleton(_clock));
        }
    }
}

/// <summary>A clock tests can move, so they can tell createdAt and updatedAt apart.</summary>
public sealed class TestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;
}
