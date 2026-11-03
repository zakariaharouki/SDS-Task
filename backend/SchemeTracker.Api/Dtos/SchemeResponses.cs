using SchemeTracker.Api.Models;

namespace SchemeTracker.Api.Dtos;

public record SchemeResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string SiteAddress { get; init; }

    public required SchemeStatus Status { get; init; }

    public required TenureMixDto TenureMix { get; init; }

    public required int TotalHomes { get; init; }

    public required int AffordableHomes { get; init; }

    public required decimal? AffordablePercent { get; init; }

    public required decimal TotalValue { get; init; }

    public required int Version { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime UpdatedAt { get; init; }
}

public record SchemeStatusSummaryResponse
{
    public required SchemeStatus Status { get; init; }

    public required int SchemeCount { get; init; }

    public required int TotalHomes { get; init; }

    public required int AffordableHomes { get; init; }

    public required decimal? AffordablePercent { get; init; }

    public required decimal TotalValue { get; init; }
}

public record SchemeSummaryResponse
{
    public required int SchemeCount { get; init; }

    public required int TotalHomes { get; init; }

    public required int AffordableHomes { get; init; }

    public required decimal? AffordablePercent { get; init; }

    public required decimal TotalValue { get; init; }

    public required IReadOnlyList<SchemeStatusSummaryResponse> ByStatus { get; init; }
}
