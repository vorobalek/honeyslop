using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Reports.Contracts;

namespace Honeyslop.Admin.Core.Services;

public interface IClientsService
{
    const int PageSize = 50;

    Task<ClientsPageDto> GetPageAsync(ClientsQuery query, CancellationToken cancellationToken);
    Task<ClientDto?> GetAsync(string ip, CancellationToken cancellationToken);
    Task<ClientAbuseDto?> GetAbuseAsync(string ip, CancellationToken cancellationToken);
    Task<RequestLogArchive?> GetArchiveAsync(string ip, CancellationToken cancellationToken);
    Task<ReportRequestDto> ReportAsync(string ip, string? senderName, CancellationToken cancellationToken);
}