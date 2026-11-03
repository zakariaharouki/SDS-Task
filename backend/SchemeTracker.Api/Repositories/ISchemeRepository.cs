using SchemeTracker.Api.Models;

namespace SchemeTracker.Api.Repositories;

/// <summary>
/// The only route to scheme storage: controllers call this and never touch MongoDB directly.
/// Further operations and the MongoDB implementation are added with the features that need them.
/// </summary>
public interface ISchemeRepository
{
    Task<Scheme?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<Scheme> CreateAsync(Scheme scheme, CancellationToken cancellationToken = default);
}
