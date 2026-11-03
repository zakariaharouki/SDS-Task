using SchemeTracker.Api.Dtos;
using SchemeTracker.Api.Interfaces;
using SchemeTracker.Api.Models;

namespace SchemeTracker.Api.Services;

public class SchemeService : ISchemeService
{
    private readonly ISchemeRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SchemeService(ISchemeRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public Task<IReadOnlyList<SchemeResponse>> ListAsync(SchemeListQuery query, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<SchemeResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<SchemeResponse> CreateAsync(CreateSchemeRequest request, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<SchemeResponse?> UpdateAsync(string id, UpdateSchemeRequest request, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<SchemeSummaryResponse> GetSummaryAsync(IReadOnlyList<SchemeStatus>? statuses, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
