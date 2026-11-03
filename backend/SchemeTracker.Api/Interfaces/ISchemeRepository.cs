using SchemeTracker.Api.Models;

namespace SchemeTracker.Api.Interfaces;

/// <summary>
/// The only route to scheme storage: services call this and never touch MongoDB directly.
/// </summary>
public interface ISchemeRepository
{
    Task<IReadOnlyList<Scheme>> ListAsync(IReadOnlyList<SchemeStatus>? statuses, string sortBy, string sortDir, CancellationToken cancellationToken = default);

    Task<Scheme?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<Scheme> CreateAsync(Scheme scheme, CancellationToken cancellationToken = default);

    Task<Scheme?> UpdateAsync(Scheme scheme, int expectedVersion, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchemeStatusTotals>> GetStatusTotalsAsync(IReadOnlyList<SchemeStatus>? statuses, CancellationToken cancellationToken = default);
}
