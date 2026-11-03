using SchemeTracker.Api.Dtos;
using SchemeTracker.Api.Models;

namespace SchemeTracker.Api.Interfaces;

public interface ISchemeService
{
    Task<IReadOnlyList<SchemeResponse>> ListAsync(SchemeListQuery query, CancellationToken cancellationToken = default);

    Task<SchemeResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<SchemeResponse> CreateAsync(CreateSchemeRequest request, CancellationToken cancellationToken = default);

    Task<SchemeResponse?> UpdateAsync(string id, UpdateSchemeRequest request, CancellationToken cancellationToken = default);

    Task<SchemeSummaryResponse> GetSummaryAsync(IReadOnlyList<SchemeStatus>? statuses, CancellationToken cancellationToken = default);
}
