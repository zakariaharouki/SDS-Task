using SchemeTracker.Api.Models;

namespace SchemeTracker.Api.Dtos;

public record CreateSchemeRequest
{
    public required string Name { get; init; }

    public required string SiteAddress { get; init; }

    public required SchemeStatus Status { get; init; }

    public required TenureMixDto TenureMix { get; init; }
}

public record UpdateSchemeRequest
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
