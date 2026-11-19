using Honeyslop.Admin.Core.Contracts;

namespace Honeyslop.Admin.Core.Services;

public interface IReportsService
{
    const int PageSize = 50;

    Task<ReportsPageDto> GetPageAsync(ReportsQuery query, CancellationToken cancellationToken);
    Task<ReportDetailsDto?> GetAsync(long id, CancellationToken cancellationToken);
}