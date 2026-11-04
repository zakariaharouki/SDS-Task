using MongoDB.Driver;
using SchemeTracker.Api.Models;

namespace SchemeTracker.Api.Repositories;

/// <summary>
/// Creates the indexes from design section 2.7 at startup. Creating an index that already exists
/// does nothing. A failure stops the API, because the unique index is what blocks duplicates.
/// </summary>
public sealed class SchemeIndexInitializer : IHostedService
{
    public static readonly Collation CaseInsensitive = new("en", strength: CollationStrength.Secondary);

    private readonly IMongoCollection<Scheme> _schemes;

    public SchemeIndexInitializer(IMongoCollection<Scheme> schemes)
    {
        _schemes = schemes;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var keys = Builders<Scheme>.IndexKeys;

        return _schemes.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<Scheme>(keys.Ascending(scheme => scheme.Status)),
                new CreateIndexModel<Scheme>(
                    keys.Ascending(scheme => scheme.Name),
                    new CreateIndexOptions { Collation = CaseInsensitive }),
                new CreateIndexModel<Scheme>(keys.Ascending(scheme => scheme.Totals.TotalHomes)),
                new CreateIndexModel<Scheme>(keys.Ascending(scheme => scheme.Totals.TotalValue)),
                new CreateIndexModel<Scheme>(
                    keys.Ascending(scheme => scheme.DuplicateKey),
                    new CreateIndexOptions { Unique = true })
            ],
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
