using SchemeTracker.Api.Models;

namespace SchemeTracker.Api.Dtos;

/// <summary>The fields create and update requests share, so both follow the same validation rules.</summary>
public interface ISchemeRequest
{
    string Name { get; }

    string SiteAddress { get; }

    SchemeStatus Status { get; }

    TenureMixDto TenureMix { get; }
}

public record CreateSchemeRequest : ISchemeRequest
{
    public required string Name { get; init; }

    public required string SiteAddress { get; init; }

    public required SchemeStatus Status { get; init; }

    public required TenureMixDto TenureMix { get; init; }
}

public record UpdateSchemeRequest : ISchemeRequest
{
    public required string Name { get; init; }

    public required string SiteAddress { get; init; }

    public required SchemeStatus Status { get; init; }

    public required TenureMixDto TenureMix { get; init; }

    public required int Version { get; init; }
}

public record SchemeListQuery
{
    public IReadOnlyList<SchemeStatus>? Status { get; init; }

    public string SortBy { get; init; } = "status";

    public string SortDir { get; init; } = "asc";
}
