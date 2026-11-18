using Honeyslop.Admin.Core.Contracts;

namespace Honeyslop.Admin.Core.Services;

public interface IRequestsService
{
    const int PageSize = 100;
    const int MaxBreakdownGroups = 200;

    Task<RequestsPageDto> GetPageAsync(RequestsQuery query, CancellationToken cancellationToken);
    Task<RequestDetailsDto?> GetAsync(long id, CancellationToken cancellationToken);
    Task<RequestsBreakdownDto> GetBreakdownAsync(RequestsBreakdownQuery query, CancellationToken cancellationToken);
}
