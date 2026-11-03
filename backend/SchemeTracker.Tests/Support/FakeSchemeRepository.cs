using SchemeTracker.Api.Interfaces;
using SchemeTracker.Api.Models;

namespace SchemeTracker.Tests.Support;

/// <summary>Stands in for MongoDB, so endpoint tests can run without a database.</summary>
public class FakeSchemeRepository : ISchemeRepository
{
    private int _nextId;

    public Task<IReadOnlyList<Scheme>> ListAsync(IReadOnlyList<SchemeStatus>? statuses, string sortBy, string sortDir, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Scheme>>([]);

    public Task<Scheme?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult<Scheme?>(null);

    public Task<Scheme> CreateAsync(Scheme scheme, CancellationToken cancellationToken = default)
    {
        scheme.Id = (++_nextId).ToString("x24");
        return Task.FromResult(scheme);
    }

    public Task<Scheme?> UpdateAsync(Scheme scheme, int expectedVersion, CancellationToken cancellationToken = default) =>
        Task.FromResult<Scheme?>(null);

    public Task<IReadOnlyList<SchemeStatusTotals>> GetStatusTotalsAsync(IReadOnlyList<SchemeStatus>? statuses, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SchemeStatusTotals>>([]);
}
