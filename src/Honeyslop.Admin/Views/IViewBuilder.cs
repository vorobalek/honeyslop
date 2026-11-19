using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Views.Contracts;

namespace Honeyslop.Admin.Views;

public interface IViewBuilder
{
    Task<EntriesViewDto> GetEntriesAsync(EntriesQuery query, CancellationToken cancellationToken);
    Task<EntryViewDto?> GetEntryAsync(long id, CancellationToken cancellationToken);
    Task<RequestsViewDto> GetRequestsAsync(RequestsQuery query, CancellationToken cancellationToken);
    Task<RequestDetailsDto?> GetRequestAsync(long id, CancellationToken cancellationToken);
    Task<ClientsPageDto> GetClientsAsync(ClientsQuery query, CancellationToken cancellationToken);
    Task<ClientDto?> GetClientAsync(string ip, CancellationToken cancellationToken);
    Task<ClientAbuseDto?> GetClientAbuseAsync(string ip, CancellationToken cancellationToken);
    Task<ReportsPageDto> GetReportsAsync(ReportsQuery query, CancellationToken cancellationToken);
    Task<ReportDetailsDto?> GetReportAsync(long id, CancellationToken cancellationToken);
    Task<GeneratorsViewDto> GetGeneratorsAsync(CancellationToken cancellationToken);
    Task<SettingsDto> GetSettingsAsync(CancellationToken cancellationToken);
}
