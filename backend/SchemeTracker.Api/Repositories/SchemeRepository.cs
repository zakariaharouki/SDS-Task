using MongoDB.Driver;
using SchemeTracker.Api.Interfaces;
using SchemeTracker.Api.Models;

namespace SchemeTracker.Api.Repositories;

public class SchemeRepository : ISchemeRepository
{
    private readonly IMongoCollection<Scheme> _schemes;

    public SchemeRepository(IMongoCollection<Scheme> schemes)
    {
        _schemes = schemes;
    }

    public Task<IReadOnlyList<Scheme>> ListAsync(IReadOnlyList<SchemeStatus>? statuses, string sortBy, string sortDir, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<Scheme?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<Scheme> CreateAsync(Scheme scheme, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<Scheme?> UpdateAsync(Scheme scheme, int expectedVersion, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<SchemeStatusTotals>> GetStatusTotalsAsync(IReadOnlyList<SchemeStatus>? statuses, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
