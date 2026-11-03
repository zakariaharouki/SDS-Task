using DotNet.Testcontainers.Configurations;
using MongoDB.Driver;
using SchemeTracker.Api.Models;
using SchemeTracker.Api.Repositories;
using Testcontainers.MongoDb;

namespace SchemeTracker.Tests.Support;

/// <summary>
/// A test that needs real MongoDB in Docker. It is skipped on a machine without Docker, but always
/// runs in CI, so a broken Docker setup there fails the build instead of skipping the tests.
/// </summary>
public sealed class MongoFactAttribute : FactAttribute
{
    public static readonly bool CanRun =
        Environment.GetEnvironmentVariable("CI") == "true"
        || TestcontainersSettings.OS.DockerEndpointAuthConfig is not null;

    public MongoFactAttribute()
    {
        if (!CanRun)
        {
            Skip = "Docker is not available. These tests run in CI.";
        }
    }
}

public sealed class MongoFixture : IAsyncLifetime
{
    private MongoDbContainer? _container;
    private MongoClient? _client;

    public async Task InitializeAsync()
    {
        if (!MongoFactAttribute.CanRun)
        {
            return;
        }

        // Not 8.0 or newer: those refuse to start on Linux kernels 6.19 to 7.0.13, which CI runners
        // currently use (SERVER-121912, SERVER-125742). Move up once runners reach 7.0.14.
        _container = new MongoDbBuilder("mongo:7.0").Build();
        await _container.StartAsync();
        _client = new MongoClient(_container.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>A collection nobody else uses, with the real indexes, so tests can't affect each other.</summary>
    public async Task<IMongoCollection<Scheme>> CreateCollectionAsync()
    {
        var collection = _client!
            .GetDatabase("test-" + Guid.NewGuid().ToString("N"))
            .GetCollection<Scheme>("schemes");

        await new SchemeIndexInitializer(collection).StartAsync(CancellationToken.None);

        return collection;
    }
}

[CollectionDefinition(Name)]
public sealed class MongoCollection : ICollectionFixture<MongoFixture>
{
    public const string Name = "MongoDB";
}
