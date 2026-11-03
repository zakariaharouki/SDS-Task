using SchemeTracker.Api.Dtos;
using SchemeTracker.Api.Models;

namespace SchemeTracker.Api.Mapping;

/// <summary>
/// Copies data between DTOs and the stored model. It holds no business rules: server-set fields
/// (id, totals, duplicateKey, version, timestamps) are left for the caller, and the affordable
/// percentage is passed in so the formula stays in SchemeCalculator.
/// </summary>
public static class SchemeMapping
{
    public static Scheme ToModel(this CreateSchemeRequest request) => new()
    {
        Name = request.Name,
        SiteAddress = request.SiteAddress,
        Status = request.Status,
        TenureMix = request.TenureMix.ToModel()
    };

    public static void ApplyTo(this UpdateSchemeRequest request, Scheme scheme)
    {
        scheme.Name = request.Name;
        scheme.SiteAddress = request.SiteAddress;
        scheme.Status = request.Status;
        scheme.TenureMix = request.TenureMix.ToModel();
    }

    public static SchemeResponse ToResponse(this Scheme scheme, decimal? affordablePercent) => new()
    {
        Id = scheme.Id,
        Name = scheme.Name,
        SiteAddress = scheme.SiteAddress,
        Status = scheme.Status,
        TenureMix = scheme.TenureMix.ToDto(),
        TotalHomes = scheme.Totals.TotalHomes,
        AffordableHomes = scheme.Totals.AffordableHomes,
        AffordablePercent = affordablePercent,
        TotalValue = scheme.Totals.TotalValue,
        Version = scheme.Version,
        CreatedAt = scheme.CreatedAt,
        UpdatedAt = scheme.UpdatedAt
    };

    public static SchemeStatusSummaryResponse ToResponse(this SchemeStatusTotals totals, decimal? affordablePercent) => new()
    {
        Status = totals.Status,
        SchemeCount = totals.SchemeCount,
        TotalHomes = totals.TotalHomes,
        AffordableHomes = totals.AffordableHomes,
        AffordablePercent = affordablePercent,
        TotalValue = totals.TotalValue
    };

    private static TenureMix ToModel(this TenureMixDto dto) => new()
    {
        SocialRent = dto.SocialRent.ToModel(),
        AffordableRent = dto.AffordableRent.ToModel(),
        SharedOwnership = dto.SharedOwnership.ToModel(),
        MarketSale = dto.MarketSale.ToModel()
    };

    private static TenureLine ToModel(this TenureLineDto dto) => new()
    {
        Homes = dto.Homes,
        AverageValue = dto.AverageValue
    };

    private static TenureMixDto ToDto(this TenureMix mix) => new()
    {
        SocialRent = mix.SocialRent.ToDto(),
        AffordableRent = mix.AffordableRent.ToDto(),
        SharedOwnership = mix.SharedOwnership.ToDto(),
        MarketSale = mix.MarketSale.ToDto()
    };

    private static TenureLineDto ToDto(this TenureLine line) => new()
    {
        Homes = line.Homes,
        AverageValue = line.AverageValue
    };
}
