using SchemeTracker.Api.Exceptions;
using SchemeTracker.Api.Interfaces;
using SchemeTracker.Api.Models;

namespace SchemeTracker.Tests.Support;

/// <summary>Stands in for MongoDB, so endpoint tests can run without a database.</summary>
public class FakeSchemeRepository : ISchemeRepository
{
    private readonly Dictionary<string, Scheme> _schemes = new();
    private int _nextId;

    public IReadOnlyList<SchemeStatus>? LastStatuses { get; private set; }

    public string? LastSortBy { get; private set; }

    public string? LastSortDir { get; private set; }

    /// <summary>Behaves like the unique index on duplicateKey. Off by default so other tests can reuse names.</summary>
    public bool EnforceUniqueKeys { get; set; }

    /// <summary>What is stored for an id, for checking fields the API doesn't return.</summary>
    public Scheme Stored(string id) => _schemes[id];

    public Task<IReadOnlyList<Scheme>> ListAsync(IReadOnlyList<SchemeStatus>? statuses, string sortBy, string sortDir, CancellationToken cancellationToken = default)
    {
        LastStatuses = statuses;
        LastSortBy = sortBy;
        LastSortDir = sortDir;
        return Task.FromResult<IReadOnlyList<Scheme>>([]);
    }

    public Task<Scheme?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_schemes.TryGetValue(id, out var scheme) ? Copy(scheme) : null);

    public Task<Scheme> CreateAsync(Scheme scheme, CancellationToken cancellationToken = default)
    {
        ThrowIfKeyIsTaken(scheme);
        scheme.Id = (++_nextId).ToString("x24");
        _schemes[scheme.Id] = Copy(scheme);
        return Task.FromResult(scheme);
    }

    // Matches design section 5.4: the update only applies when the id and version both match.
    public Task<Scheme?> UpdateAsync(Scheme scheme, int expectedVersion, CancellationToken cancellationToken = default)
    {
        if (!_schemes.TryGetValue(scheme.Id, out var stored) || stored.Version != expectedVersion)
        {
            return Task.FromResult<Scheme?>(null);
        }

        ThrowIfKeyIsTaken(scheme);

        var updated = Copy(scheme);
        updated.Version = expectedVersion + 1;
        _schemes[scheme.Id] = updated;
        return Task.FromResult<Scheme?>(Copy(updated));
    }

    public Task<IReadOnlyList<SchemeStatusTotals>> GetStatusTotalsAsync(IReadOnlyList<SchemeStatus>? statuses, CancellationToken cancellationToken = default)
    {
        LastStatuses = statuses;
        return Task.FromResult<IReadOnlyList<SchemeStatusTotals>>([]);
    }

    private void ThrowIfKeyIsTaken(Scheme scheme)
    {
        if (EnforceUniqueKeys
            && _schemes.Values.Any(other => other.Id != scheme.Id && other.DuplicateKey == scheme.DuplicateKey))
        {
            throw new DuplicateSchemeException();
        }
    }

    // The service changes the object it loads, so a copy keeps "stored" separate, as a database would.
    private static Scheme Copy(Scheme scheme) => new()
    {
        Id = scheme.Id,
        Name = scheme.Name,
        SiteAddress = scheme.SiteAddress,
        Status = scheme.Status,
        TenureMix = scheme.TenureMix,
        Totals = scheme.Totals,
        DuplicateKey = scheme.DuplicateKey,
        Version = scheme.Version,
        CreatedAt = scheme.CreatedAt,
        UpdatedAt = scheme.UpdatedAt
    };
}
