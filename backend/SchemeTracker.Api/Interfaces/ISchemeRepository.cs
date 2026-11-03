using SchemeTracker.Api.Exceptions;
using SchemeTracker.Api.Models;

namespace SchemeTracker.Api.Interfaces;

/// <summary>
/// The only route to scheme storage: services call this and never touch MongoDB directly.
/// </summary>
public interface ISchemeRepository
{
    Task<IReadOnlyList<Scheme>> ListAsync(IReadOnlyList<SchemeStatus>? statuses, string sortBy, string sortDir, CancellationToken cancellationToken = default);

    Task<Scheme?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <exception cref="DuplicateSchemeException">Another scheme has the same duplicate key.</exception>
    Task<Scheme> CreateAsync(Scheme scheme, CancellationToken cancellationToken = default);

    /// <summary>Saves the scheme only if its stored version matches, and increments the version.</summary>
    /// <returns>The saved scheme, or null if no scheme matched the id and version.</returns>
    /// <exception cref="DuplicateSchemeException">Another scheme has the same duplicate key.</exception>
    Task<Scheme?> UpdateAsync(Scheme scheme, int expectedVersion, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchemeStatusTotals>> GetStatusTotalsAsync(IReadOnlyList<SchemeStatus>? statuses, CancellationToken cancellationToken = default);
}
