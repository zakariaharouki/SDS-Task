using System.Text.RegularExpressions;
using SchemeTracker.Api.Dtos;
using SchemeTracker.Api.Interfaces;
using SchemeTracker.Api.Mapping;
using SchemeTracker.Api.Models;
using SchemeTracker.Api.Mapping;

namespace SchemeTracker.Api.Services;

public class SchemeService : ISchemeService
{
    private readonly ISchemeRepository _repository;
    private readonly TimeProvider _timeProvider;


    public SchemeService(
        ISchemeRepository repository,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }
    private static decimal? CalculateAffordablePercent(
        int affordableHomes,
        int totalHomes)
    {
        if (totalHomes == 0)
        {
            return null;
        }

        decimal percentage = (decimal)affordableHomes / totalHomes * 100;

        return Math.Round(percentage, 1);
    }
    public async Task<IReadOnlyList<SchemeResponse>> ListAsync(
        SchemeListQuery query,
        CancellationToken cancellationToken = default)
    {
        var schemes = await _repository.ListAsync(
            query.Status,
            query.SortBy,
            query.SortDir,
            cancellationToken);

        var responses = new List<SchemeResponse>();

        foreach (var scheme in schemes)
        {
            decimal? percentage = CalculateAffordablePercent(
                scheme.Totals.AffordableHomes,
                scheme.Totals.TotalHomes);

            var response = scheme.ToResponse(percentage);

            responses.Add(response);
        }

        return responses;
    }
    public async Task<SchemeResponse?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var scheme = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (scheme == null)
        {
            return null;
        }

        decimal? percentage = CalculateAffordablePercent(
            scheme.Totals.AffordableHomes,
            scheme.Totals.TotalHomes);

        return scheme.ToResponse(percentage);
    }

    private static SchemeTotals CalculateTotals(TenureMix mix)
    {
        var totals = new SchemeTotals();

        totals.TotalHomes =
            mix.SocialRent.Homes +
            mix.AffordableRent.Homes +
            mix.SharedOwnership.Homes +
            mix.MarketSale.Homes;

        totals.AffordableHomes =
            mix.SocialRent.Homes +
            mix.AffordableRent.Homes +
            mix.SharedOwnership.Homes;

        totals.TotalValue =
            mix.SocialRent.Homes * mix.SocialRent.AverageValue +
            mix.AffordableRent.Homes * mix.AffordableRent.AverageValue +
            mix.SharedOwnership.Homes * mix.SharedOwnership.AverageValue +
            mix.MarketSale.Homes * mix.MarketSale.AverageValue;

        return totals;
    }

    public async Task<SchemeResponse> CreateAsync(
    CreateSchemeRequest request,
    CancellationToken cancellationToken = default)
    {
        // Convert the request into a database model.
        var scheme = request.ToModel();

        // Remove spaces at the beginning and end.
        scheme.Name = scheme.Name.Trim();
        scheme.SiteAddress = scheme.SiteAddress.Trim();

        // Calculate the figures on the server.
        scheme.Totals = CalculateTotals(scheme.TenureMix);

        // Set the initial version and timestamps.
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        scheme.Version = 1;
        scheme.CreatedAt = now;
        scheme.UpdatedAt = now;

        // Save the scheme.
        var created = await _repository.CreateAsync(
            scheme,
            cancellationToken);

        // Calculate the percentage for the response.
        decimal? percentage = CalculateAffordablePercent(
            created.Totals.AffordableHomes,
            created.Totals.TotalHomes);

        return created.ToResponse(percentage);
    }
    public async Task<SchemeResponse?> UpdateAsync(
    string id,
    UpdateSchemeRequest request,
    CancellationToken cancellationToken = default)
    {
        // Find the existing scheme.
        var scheme = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (scheme == null)
        {
            return null;
        }

        // Copy the editable fields from the request.
        request.ApplyTo(scheme);

        scheme.Name = scheme.Name.Trim();
        scheme.SiteAddress = scheme.SiteAddress.Trim();

        // Recalculate because the tenure mix may have changed.
        scheme.Totals = CalculateTotals(scheme.TenureMix);

        // Keep CreatedAt unchanged.
        scheme.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        // The repository checks the version and increments it atomically.
        var updated = await _repository.UpdateAsync(
            scheme,
            request.Version,
            cancellationToken);

        if (updated == null)
        {
            return null;
        }

        decimal? percentage = CalculateAffordablePercent(
            updated.Totals.AffordableHomes,
            updated.Totals.TotalHomes);

        return updated.ToResponse(percentage);
    }
    public async Task<SchemeSummaryResponse> GetSummaryAsync(
    IReadOnlyList<SchemeStatus>? statuses,
    CancellationToken cancellationToken = default)
    {
        var statusTotals = await _repository.GetStatusTotalsAsync(
            statuses,
            cancellationToken);

        int schemeCount = 0;
        int totalHomes = 0;
        int affordableHomes = 0;
        decimal totalValue = 0;

        var byStatus = new List<SchemeStatusSummaryResponse>();

        SchemeStatus[] allStatuses =
        [
            SchemeStatus.Appraisal,
        SchemeStatus.Planning,
        SchemeStatus.OnSite,
        SchemeStatus.Complete
        ];

        foreach (var status in allStatuses)
        {
            var totals = statusTotals.FirstOrDefault(
                item => item.Status == status);

            // Show zeros when this status has no schemes.
            if (totals == null)
            {
                totals = new SchemeStatusTotals
                {
                    Status = status
                };
            }

            schemeCount += totals.SchemeCount;
            totalHomes += totals.TotalHomes;
            affordableHomes += totals.AffordableHomes;
            totalValue += totals.TotalValue;

            decimal? percentage = CalculateAffordablePercent(
                totals.AffordableHomes,
                totals.TotalHomes);

            byStatus.Add(totals.ToResponse(percentage));
        }

        return new SchemeSummaryResponse
        {
            SchemeCount = schemeCount,
            TotalHomes = totalHomes,
            AffordableHomes = affordableHomes,
            TotalValue = totalValue,
            AffordablePercent = CalculateAffordablePercent(
                affordableHomes,
                totalHomes),
            ByStatus = byStatus
        };
    }
}


