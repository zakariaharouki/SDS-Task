using MongoDB.Bson;
using MongoDB.Driver;
using SchemeTracker.Api.Exceptions;
using SchemeTracker.Api.Interfaces;
using SchemeTracker.Api.Models;

namespace SchemeTracker.Api.Repositories;

public class SchemeRepository : ISchemeRepository
{
    private const int DuplicateKeyErrorCode = 11000;
    private const string NameField = "name";
    private const string StatusOrderField = "statusOrder";

    private static readonly Dictionary<string, string> SortFields = new()
    {
        ["status"] = StatusOrderField,
        ["name"] = NameField,
        ["totalHomes"] = "totals.totalHomes",
        ["totalValue"] = "totals.totalValue",
        ["updatedAt"] = "updatedAt"
    };

    private readonly IMongoCollection<Scheme> _schemes;

    public SchemeRepository(IMongoCollection<Scheme> schemes)
    {
        _schemes = schemes;
    }

    public async Task<IReadOnlyList<Scheme>> ListAsync(IReadOnlyList<SchemeStatus>? statuses, string sortBy, string sortDir, CancellationToken cancellationToken = default)
    {
        var sortField = SortFields[sortBy];
        var sortsByStatus = sortField == StatusOrderField;

        var pipeline = _schemes
            .Aggregate(new AggregateOptions { Collation = SchemeIndexInitializer.CaseInsensitive })
            .Match(StatusFilter(statuses));

        if (sortsByStatus)
        {
            pipeline = pipeline.AppendStage<Scheme>(AddStatusOrderStage());
        }

        pipeline = pipeline.Sort(StableSort(sortField, sortDir == "desc" ? -1 : 1));

        if (sortsByStatus)
        {
            pipeline = pipeline.AppendStage<Scheme>(new BsonDocument("$unset", StatusOrderField));
        }

        return await pipeline.ToListAsync(cancellationToken);
    }

    public async Task<Scheme?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        // A malformed id can't match anything, so it is treated as not found (design section 4.2).
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        return await _schemes
            .Find(Builders<Scheme>.Filter.Eq(scheme => scheme.Id, id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Scheme> CreateAsync(Scheme scheme, CancellationToken cancellationToken = default)
    {
        try
        {
            await _schemes.InsertOneAsync(scheme, cancellationToken: cancellationToken);
            return scheme;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Code == DuplicateKeyErrorCode)
        {
            throw new DuplicateSchemeException(exception);
        }
    }

    public async Task<Scheme?> UpdateAsync(Scheme scheme, int expectedVersion, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(scheme.Id, out _))
        {
            return null;
        }

        var filter = Builders<Scheme>.Filter.And(
            Builders<Scheme>.Filter.Eq(stored => stored.Id, scheme.Id),
            Builders<Scheme>.Filter.Eq(stored => stored.Version, expectedVersion));

        var update = Builders<Scheme>.Update
            .Set(stored => stored.Name, scheme.Name)
            .Set(stored => stored.SiteAddress, scheme.SiteAddress)
            .Set(stored => stored.Status, scheme.Status)
            .Set(stored => stored.TenureMix, scheme.TenureMix)
            .Set(stored => stored.Totals, scheme.Totals)
            .Set(stored => stored.DuplicateKey, scheme.DuplicateKey)
            .Set(stored => stored.UpdatedAt, scheme.UpdatedAt)
            .Inc(stored => stored.Version, 1);

        try
        {
            return await _schemes.FindOneAndUpdateAsync<Scheme?>(
                filter,
                update,
                new FindOneAndUpdateOptions<Scheme, Scheme?> { ReturnDocument = ReturnDocument.After },
                cancellationToken);
        }
        catch (MongoCommandException exception) when (exception.Code == DuplicateKeyErrorCode)
        {
            throw new DuplicateSchemeException(exception);
        }
    }

    public async Task<IReadOnlyList<SchemeStatusTotals>> GetStatusTotalsAsync(IReadOnlyList<SchemeStatus>? statuses, CancellationToken cancellationToken = default)
    {
        var groups = await _schemes
            .Aggregate()
            .Match(StatusFilter(statuses))
            .Group(new BsonDocument
            {
                { "_id", "$status" },
                { "schemeCount", new BsonDocument("$sum", 1) },
                { "totalHomes", new BsonDocument("$sum", "$totals.totalHomes") },
                { "affordableHomes", new BsonDocument("$sum", "$totals.affordableHomes") },
                { "totalValue", new BsonDocument("$sum", "$totals.totalValue") }
            })
            .ToListAsync(cancellationToken);

        return groups.Select(group => new SchemeStatusTotals
        {
            Status = Enum.Parse<SchemeStatus>(group["_id"].AsString),
            SchemeCount = group["schemeCount"].ToInt32(),
            TotalHomes = group["totalHomes"].ToInt32(),
            AffordableHomes = group["affordableHomes"].ToInt32(),
            TotalValue = Decimal128.ToDecimal(group["totalValue"].AsDecimal128)
        }).ToList();
    }

    private static FilterDefinition<Scheme> StatusFilter(IReadOnlyList<SchemeStatus>? statuses) =>
        statuses is null
            ? Builders<Scheme>.Filter.Empty
            : Builders<Scheme>.Filter.In(scheme => scheme.Status, statuses);

    // Adds each scheme's position in the pipeline, because sorting the names would be alphabetical.
    private static BsonDocument AddStatusOrderStage()
    {
        var pipelineOrder = new BsonArray(SchemeStatusNames.PipelineOrder.Select(status => status.ToString()));

        return new BsonDocument("$addFields", new BsonDocument(
            StatusOrderField,
            new BsonDocument("$indexOfArray", new BsonArray { pipelineOrder, "$status" })));
    }

    // The tie-breakers stop rows with equal values moving between requests (design section 4.1).
    private static BsonDocument StableSort(string field, int direction)
    {
        var sort = new BsonDocument(field, direction);

        if (field != NameField)
        {
            sort.Add(NameField, 1);
        }

        sort.Add("_id", 1);
        return sort;
    }
}
