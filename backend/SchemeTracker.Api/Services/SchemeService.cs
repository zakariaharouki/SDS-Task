using System.Text.RegularExpressions;
using FluentValidation;
using SchemeTracker.Api.Dtos;
using SchemeTracker.Api.Exceptions;
using SchemeTracker.Api.Interfaces;
using SchemeTracker.Api.Mapping;
using SchemeTracker.Api.Models;
using SchemeTracker.Api.Mapping;

namespace SchemeTracker.Api.Services;

public class SchemeService : ISchemeService
{
    private readonly ISchemeRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<ISchemeRequest> _validator;
    private readonly IValidator<SchemeListQuery> _queryValidator;


    public SchemeService(
        ISchemeRepository repository,
        TimeProvider timeProvider,
        IValidator<ISchemeRequest> validator,
        IValidator<SchemeListQuery> queryValidator)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _validator = validator;
        _queryValidator = queryValidator;
    }

    // Whole milliseconds, because that's all MongoDB stores. Otherwise a create response
    // wouldn't match the same scheme when it is read back.
    private DateTime UtcNow()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMillisecond));
    }

    // "No filter" reaches the repository as null, however the caller expressed it.
    private static IReadOnlyList<SchemeStatus>? NoFilterAsNull(IReadOnlyList<SchemeStatus>? statuses) =>
        statuses is { Count: > 0 } ? statuses : null;

    public async Task<IReadOnlyList<SchemeResponse>> ListAsync(
        SchemeListQuery query,
        CancellationToken cancellationToken = default)
    {
        await _queryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var schemes = await _repository.ListAsync(
            NoFilterAsNull(query.Status),
            query.SortBy,
            query.SortDir,
            cancellationToken);

        var responses = new List<SchemeResponse>();

        foreach (var scheme in schemes)
        {
            decimal? percentage = SchemeCalculator.CalculateAffordablePercent(
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

        decimal? percentage = SchemeCalculator.CalculateAffordablePercent(
            scheme.Totals.AffordableHomes,
            scheme.Totals.TotalHomes);

        return scheme.ToResponse(percentage);
    }

    public async Task<SchemeResponse> CreateAsync(
    CreateSchemeRequest request,
    CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        // Convert the request into a database model.
        var scheme = request.ToModel();

        // Remove spaces at the beginning and end.
        scheme.Name = scheme.Name.Trim();
        scheme.SiteAddress = scheme.SiteAddress.Trim();

        scheme.DuplicateKey = SchemeDuplicateKey.Create(scheme.Name, scheme.SiteAddress);

        // Calculate the figures on the server.
        scheme.Totals = SchemeCalculator.CalculateTotals(scheme.TenureMix);

        // Set the initial version and timestamps.
        var now = UtcNow();

        scheme.Version = 1;
        scheme.CreatedAt = now;
        scheme.UpdatedAt = now;

        // Save the scheme.
        var created = await _repository.CreateAsync(
            scheme,
            cancellationToken);

        // Calculate the percentage for the response.
        decimal? percentage = SchemeCalculator.CalculateAffordablePercent(
            created.Totals.AffordableHomes,
            created.Totals.TotalHomes);

        return created.ToResponse(percentage);
    }
    public async Task<SchemeResponse?> UpdateAsync(
    string id,
    UpdateSchemeRequest request,
    CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

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

        scheme.DuplicateKey = SchemeDuplicateKey.Create(scheme.Name, scheme.SiteAddress);

        // Recalculate because the tenure mix may have changed.
        scheme.Totals = SchemeCalculator.CalculateTotals(scheme.TenureMix);

        // Keep CreatedAt unchanged.
        scheme.UpdatedAt = UtcNow();

        // The repository checks the version and increments it atomically.
        var updated = await _repository.UpdateAsync(
            scheme,
            request.Version,
            cancellationToken);

        // The scheme was found above, so no match here means the version was out of date.
        if (updated == null)
        {
            throw new SchemeVersionConflictException();
        }

        decimal? percentage = SchemeCalculator.CalculateAffordablePercent(
            updated.Totals.AffordableHomes,
            updated.Totals.TotalHomes);

        return updated.ToResponse(percentage);
    }
    public async Task<SchemeSummaryResponse> GetSummaryAsync(
    IReadOnlyList<SchemeStatus>? statuses,
    CancellationToken cancellationToken = default)
    {
        var statusTotals = await _repository.GetStatusTotalsAsync(
            NoFilterAsNull(statuses),
            cancellationToken);

        int schemeCount = 0;
        int totalHomes = 0;
        int affordableHomes = 0;
        decimal totalValue = 0;

        var byStatus = new List<SchemeStatusSummaryResponse>();

        foreach (var status in SchemeStatusNames.PipelineOrder)
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

            decimal? percentage = SchemeCalculator.CalculateAffordablePercent(
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
            AffordablePercent = SchemeCalculator.CalculateAffordablePercent(
                affordableHomes,
                totalHomes),
            ByStatus = byStatus
        };
    }
}


