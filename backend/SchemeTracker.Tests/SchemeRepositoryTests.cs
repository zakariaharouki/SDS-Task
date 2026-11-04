using MongoDB.Bson;
using MongoDB.Driver;
using SchemeTracker.Api.Exceptions;
using SchemeTracker.Api.Models;
using SchemeTracker.Api.Repositories;
using SchemeTracker.Tests.Support;

namespace SchemeTracker.Tests;

[Collection(MongoCollection.Name)]
public class SchemeRepositoryTests
{
    private static readonly DateTime CreatedAt = new(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);

    private readonly MongoFixture _mongo;

    public SchemeRepositoryTests(MongoFixture mongo)
    {
        _mongo = mongo;
    }

    private async Task<(SchemeRepository Repository, IMongoCollection<Scheme> Collection)> CreateRepositoryAsync()
    {
        var collection = await _mongo.CreateCollectionAsync();
        return (new SchemeRepository(collection), collection);
    }

    private static Scheme NewScheme(
        string name = "Riverside",
        SchemeStatus status = SchemeStatus.Planning,
        int totalHomes = 10,
        int affordableHomes = 6,
        decimal totalValue = 1000m,
        string? duplicateKey = null) => new()
    {
        Name = name,
        SiteAddress = "Leeds",
        Status = status,
        TenureMix = new TenureMix
        {
            SocialRent = new TenureLine { Homes = affordableHomes, AverageValue = 123456.78m },
            MarketSale = new TenureLine { Homes = totalHomes - affordableHomes, AverageValue = 0.10m }
        },
        Totals = new SchemeTotals { TotalHomes = totalHomes, AffordableHomes = affordableHomes, TotalValue = totalValue },
        DuplicateKey = duplicateKey ?? Guid.NewGuid().ToString(),
        Version = 1,
        CreatedAt = CreatedAt,
        UpdatedAt = CreatedAt
    };

    private static async Task<IReadOnlyList<string>> NamesAsync(
        SchemeRepository repository,
        string sortBy,
        string sortDir = "asc",
        IReadOnlyList<SchemeStatus>? statuses = null)
    {
        var schemes = await repository.ListAsync(statuses, sortBy, sortDir);
        return schemes.Select(scheme => scheme.Name).ToList();
    }

    [MongoFact]
    public async Task Create_generates_an_id_and_get_returns_the_same_data()
    {
        var (repository, _) = await CreateRepositoryAsync();

        var created = await repository.CreateAsync(NewScheme());
        var found = await repository.GetByIdAsync(created.Id);

        Assert.True(ObjectId.TryParse(created.Id, out _));
        Assert.NotNull(found);
        Assert.Equal("Riverside", found.Name);
        Assert.Equal(SchemeStatus.Planning, found.Status);
        Assert.Equal(1, found.Version);
        Assert.Equal(CreatedAt, found.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, found.CreatedAt.Kind);
    }

    [MongoFact]
    public async Task Money_round_trips_exactly()
    {
        var (repository, _) = await CreateRepositoryAsync();

        var created = await repository.CreateAsync(NewScheme(totalValue: 37400000.55m));
        var found = await repository.GetByIdAsync(created.Id);

        Assert.Equal(123456.78m, found!.TenureMix.SocialRent.AverageValue);
        Assert.Equal(0.10m, found.TenureMix.MarketSale.AverageValue);
        Assert.Equal(37400000.55m, found.Totals.TotalValue);
    }

    [MongoFact]
    public async Task Stored_document_uses_the_design_shape()
    {
        var (repository, collection) = await CreateRepositoryAsync();

        var created = await repository.CreateAsync(NewScheme(status: SchemeStatus.OnSite));
        var document = await collection.Database
            .GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName)
            .Find(new BsonDocument("_id", ObjectId.Parse(created.Id)))
            .SingleAsync();

        Assert.Equal("OnSite", document["status"].AsString);
        Assert.Equal(BsonType.Decimal128, document["totals"]["totalValue"].BsonType);
        Assert.Equal(BsonType.Decimal128, document["tenureMix"]["socialRent"]["averageValue"].BsonType);
    }

    [MongoFact]
    public async Task Unknown_id_returns_null()
    {
        var (repository, _) = await CreateRepositoryAsync();

        Assert.Null(await repository.GetByIdAsync(ObjectId.GenerateNewId().ToString()));
    }

    [MongoFact]
    public async Task Malformed_id_returns_null_instead_of_throwing()
    {
        var (repository, _) = await CreateRepositoryAsync();

        Assert.Null(await repository.GetByIdAsync("not-an-objectid"));
    }

    [MongoFact]
    public async Task Duplicate_key_on_create_throws_the_conflict_exception()
    {
        var (repository, _) = await CreateRepositoryAsync();
        await repository.CreateAsync(NewScheme(duplicateKey: "riverside|leeds"));

        await Assert.ThrowsAsync<DuplicateSchemeException>(
            () => repository.CreateAsync(NewScheme(duplicateKey: "riverside|leeds")));
    }

    [MongoFact]
    public async Task Concurrent_creates_of_a_duplicate_save_exactly_one()
    {
        var (repository, collection) = await CreateRepositoryAsync();

        var attempts = Enumerable.Range(0, 10).Select(async _ =>
        {
            try
            {
                await repository.CreateAsync(NewScheme(duplicateKey: "same|key"));
                return true;
            }
            catch (DuplicateSchemeException)
            {
                return false;
            }
        });

        var results = await Task.WhenAll(attempts);

        Assert.Equal(1, results.Count(saved => saved));
        Assert.Equal(1, await collection.CountDocumentsAsync(FilterDefinition<Scheme>.Empty));
    }

    [MongoFact]
    public async Task Update_saves_changes_and_increments_the_version()
    {
        var (repository, _) = await CreateRepositoryAsync();
        var scheme = await repository.CreateAsync(NewScheme());
        var updatedAt = CreatedAt.AddDays(1);

        scheme.Name = "Renamed";
        scheme.Status = SchemeStatus.Complete;
        scheme.Totals = new SchemeTotals { TotalHomes = 5, AffordableHomes = 1, TotalValue = 50m };
        scheme.UpdatedAt = updatedAt;

        var updated = await repository.UpdateAsync(scheme, expectedVersion: 1);
        var found = await repository.GetByIdAsync(scheme.Id);

        Assert.Equal(2, updated!.Version);
        Assert.Equal("Renamed", updated.Name);
        Assert.Equal(2, found!.Version);
        Assert.Equal(SchemeStatus.Complete, found.Status);
        Assert.Equal(5, found.Totals.TotalHomes);
        Assert.Equal(updatedAt, found.UpdatedAt);
        Assert.Equal(CreatedAt, found.CreatedAt);
    }

    [MongoFact]
    public async Task Update_with_a_stale_version_returns_null_and_changes_nothing()
    {
        var (repository, _) = await CreateRepositoryAsync();
        var scheme = await repository.CreateAsync(NewScheme());

        scheme.Name = "Renamed";
        var updated = await repository.UpdateAsync(scheme, expectedVersion: 99);
        var found = await repository.GetByIdAsync(scheme.Id);

        Assert.Null(updated);
        Assert.Equal("Riverside", found!.Name);
        Assert.Equal(1, found.Version);
    }

    [MongoFact]
    public async Task Concurrent_updates_from_one_version_save_exactly_one()
    {
        var (repository, _) = await CreateRepositoryAsync();
        var scheme = await repository.CreateAsync(NewScheme());

        var results = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => repository.UpdateAsync(scheme, expectedVersion: 1)));

        Assert.Equal(1, results.Count(updated => updated is not null));
        Assert.Equal(2, (await repository.GetByIdAsync(scheme.Id))!.Version);
    }

    [MongoFact]
    public async Task Update_keeping_its_own_key_does_not_clash_with_itself()
    {
        var (repository, _) = await CreateRepositoryAsync();
        var scheme = await repository.CreateAsync(NewScheme(duplicateKey: "riverside|leeds"));

        Assert.NotNull(await repository.UpdateAsync(scheme, expectedVersion: 1));
    }

    [MongoFact]
    public async Task Update_onto_another_schemes_key_throws_the_conflict_exception()
    {
        var (repository, _) = await CreateRepositoryAsync();
        await repository.CreateAsync(NewScheme(duplicateKey: "taken|key"));
        var scheme = await repository.CreateAsync(NewScheme(duplicateKey: "free|key"));

        scheme.DuplicateKey = "taken|key";

        await Assert.ThrowsAsync<DuplicateSchemeException>(() => repository.UpdateAsync(scheme, expectedVersion: 1));
    }

    [MongoFact]
    public async Task Status_sort_uses_pipeline_order_not_alphabetical()
    {
        var (repository, _) = await CreateRepositoryAsync();
        await repository.CreateAsync(NewScheme(name: "C", status: SchemeStatus.Complete));
        await repository.CreateAsync(NewScheme(name: "O", status: SchemeStatus.OnSite));
        await repository.CreateAsync(NewScheme(name: "A", status: SchemeStatus.Appraisal));
        await repository.CreateAsync(NewScheme(name: "P", status: SchemeStatus.Planning));

        Assert.Equal(["A", "P", "O", "C"], await NamesAsync(repository, "status"));
        Assert.Equal(["C", "O", "P", "A"], await NamesAsync(repository, "status", "desc"));
    }

    [MongoFact]
    public async Task Status_sort_returns_schemes_that_can_be_read_back()
    {
        var (repository, _) = await CreateRepositoryAsync();
        await repository.CreateAsync(NewScheme());

        var schemes = await repository.ListAsync(null, "status", "asc");

        Assert.Equal(SchemeStatus.Planning, Assert.Single(schemes).Status);
    }

    [MongoFact]
    public async Task Name_sort_ignores_case()
    {
        var (repository, _) = await CreateRepositoryAsync();
        await repository.CreateAsync(NewScheme(name: "Beta"));
        await repository.CreateAsync(NewScheme(name: "alpha"));
        await repository.CreateAsync(NewScheme(name: "Charlie"));

        Assert.Equal(["alpha", "Beta", "Charlie"], await NamesAsync(repository, "name"));
        Assert.Equal(["Charlie", "Beta", "alpha"], await NamesAsync(repository, "name", "desc"));
    }

    [MongoFact]
    public async Task Total_value_sort_is_numeric()
    {
        var (repository, _) = await CreateRepositoryAsync();
        await repository.CreateAsync(NewScheme(name: "Big", totalValue: 1000m));
        await repository.CreateAsync(NewScheme(name: "Small", totalValue: 9.5m));
        await repository.CreateAsync(NewScheme(name: "Middle", totalValue: 100m));

        Assert.Equal(["Small", "Middle", "Big"], await NamesAsync(repository, "totalValue"));
        Assert.Equal(["Big", "Middle", "Small"], await NamesAsync(repository, "totalValue", "desc"));
    }

    [MongoFact]
    public async Task Total_homes_sort_is_numeric()
    {
        var (repository, _) = await CreateRepositoryAsync();
        await repository.CreateAsync(NewScheme(name: "Big", totalHomes: 100));
        await repository.CreateAsync(NewScheme(name: "Small", totalHomes: 9));

        Assert.Equal(["Small", "Big"], await NamesAsync(repository, "totalHomes"));
    }

    [MongoFact]
    public async Task Updated_at_sort_uses_the_timestamp()
    {
        var (repository, _) = await CreateRepositoryAsync();
        var older = NewScheme(name: "Older");
        var newer = NewScheme(name: "Newer");
        newer.UpdatedAt = CreatedAt.AddDays(1);
        await repository.CreateAsync(newer);
        await repository.CreateAsync(older);

        Assert.Equal(["Older", "Newer"], await NamesAsync(repository, "updatedAt"));
    }

    [MongoFact]
    public async Task Equal_values_are_tie_broken_by_name_in_either_direction()
    {
        var (repository, _) = await CreateRepositoryAsync();
        await repository.CreateAsync(NewScheme(name: "B", totalValue: 100m));
        await repository.CreateAsync(NewScheme(name: "A", totalValue: 100m));
        await repository.CreateAsync(NewScheme(name: "C", totalValue: 200m));

        Assert.Equal(["A", "B", "C"], await NamesAsync(repository, "totalValue"));
        Assert.Equal(["C", "A", "B"], await NamesAsync(repository, "totalValue", "desc"));
    }

    [MongoFact]
    public async Task Equal_names_are_tie_broken_by_id()
    {
        var (repository, _) = await CreateRepositoryAsync();
        var first = await repository.CreateAsync(NewScheme(name: "Same"));
        var second = await repository.CreateAsync(NewScheme(name: "Same"));

        var schemes = await repository.ListAsync(null, "name", "asc");

        Assert.Equal([first.Id, second.Id], schemes.Select(scheme => scheme.Id));
    }

    [MongoFact]
    public async Task List_filters_by_several_statuses()
    {
        var (repository, _) = await CreateRepositoryAsync();
        await repository.CreateAsync(NewScheme(name: "A", status: SchemeStatus.Appraisal));
        await repository.CreateAsync(NewScheme(name: "P", status: SchemeStatus.Planning));
        await repository.CreateAsync(NewScheme(name: "O", status: SchemeStatus.OnSite));

        var names = await NamesAsync(repository, "status", statuses: [SchemeStatus.Planning, SchemeStatus.OnSite]);

        Assert.Equal(["P", "O"], names);
    }

    [MongoFact]
    public async Task Status_totals_add_up_per_status_without_losing_pence()
    {
        var (repository, _) = await CreateRepositoryAsync();
        await repository.CreateAsync(NewScheme(status: SchemeStatus.Planning, totalHomes: 10, affordableHomes: 6, totalValue: 0.10m));
        await repository.CreateAsync(NewScheme(status: SchemeStatus.Planning, totalHomes: 20, affordableHomes: 5, totalValue: 0.20m));
        await repository.CreateAsync(NewScheme(status: SchemeStatus.OnSite, totalHomes: 1, affordableHomes: 1, totalValue: 123456.78m));

        var totals = await repository.GetStatusTotalsAsync(null);

        var planning = totals.Single(row => row.Status == SchemeStatus.Planning);
        Assert.Equal(2, totals.Count);
        Assert.Equal(2, planning.SchemeCount);
        Assert.Equal(30, planning.TotalHomes);
        Assert.Equal(11, planning.AffordableHomes);
        Assert.Equal(0.30m, planning.TotalValue);
        Assert.Equal(123456.78m, totals.Single(row => row.Status == SchemeStatus.OnSite).TotalValue);
    }

    [MongoFact]
    public async Task Status_totals_follow_the_filter()
    {
        var (repository, _) = await CreateRepositoryAsync();
        await repository.CreateAsync(NewScheme(status: SchemeStatus.Planning));
        await repository.CreateAsync(NewScheme(status: SchemeStatus.OnSite));

        var totals = await repository.GetStatusTotalsAsync([SchemeStatus.OnSite]);

        Assert.Equal(SchemeStatus.OnSite, Assert.Single(totals).Status);
    }

    [MongoFact]
    public async Task Status_totals_are_empty_for_an_empty_collection()
    {
        var (repository, _) = await CreateRepositoryAsync();

        Assert.Empty(await repository.GetStatusTotalsAsync(null));
    }

    [MongoFact]
    public async Task Creating_the_indexes_again_changes_nothing()
    {
        var collection = await _mongo.CreateCollectionAsync();

        await new SchemeIndexInitializer(collection).StartAsync(CancellationToken.None);

        var indexes = await (await collection.Indexes.ListAsync()).ToListAsync();
        var unique = indexes.Single(index => index["key"].AsBsonDocument.Contains("duplicateKey"));
        var name = indexes.Single(index => index["key"].AsBsonDocument.Contains("name"));

        // _id plus the five from design section 2.7.
        Assert.Equal(6, indexes.Count);
        Assert.True(unique["unique"].ToBoolean());
        Assert.Equal(2, name["collation"]["strength"].ToInt32());
    }
}
