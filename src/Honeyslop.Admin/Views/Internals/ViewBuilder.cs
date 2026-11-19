using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Extensions;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Admin.Views.Contracts;
using Honeyslop.Data.Entities;

namespace Honeyslop.Admin.Views.Internals;

internal sealed class ViewBuilder(
    IEntriesService entriesService,
    IRequestsService requestsService,
    IClientsService clientsService,
    IReportsService reportsService,
    IBlockedContactsService blockedContactsService,
    IGeneratorsService generatorsService,
    ISettingsService settingsService) : IViewBuilder
{
    public async Task<EntriesViewDto> GetEntriesAsync(EntriesQuery query, CancellationToken cancellationToken)
    {
        var page = await entriesService.GetPageAsync(query, cancellationToken);
        return new EntriesViewDto(page.Totals, Rows(page.Entries, ToRow), page.AsOf);
    }

    public async Task<EntryViewDto?> GetEntryAsync(long id, CancellationToken cancellationToken)
    {
        return await entriesService.GetAsync(id, cancellationToken) is { } details
            ? new EntryViewDto(
                details.Entry,
                details.Entry.State,
                details.Entry.CanPreview,
                details.Country,
                details.CurrentId,
                details.Output,
                details.DefaultResponseDelayMilliseconds,
                details.AsOf)
            : null;
    }

    public async Task<RequestsViewDto> GetRequestsAsync(RequestsQuery query, CancellationToken cancellationToken)
    {
        var page = await requestsService.GetPageAsync(query, cancellationToken);
        return new RequestsViewDto(Rows(page.Requests, ToRow), page.Countries, page.Delays, page.LearnedDelays, page.AsOf);
    }

    public Task<RequestDetailsDto?> GetRequestAsync(long id, CancellationToken cancellationToken)
    {
        return requestsService.GetAsync(id, cancellationToken);
    }

    public Task<ClientsPageDto> GetClientsAsync(ClientsQuery query, CancellationToken cancellationToken)
    {
        return clientsService.GetPageAsync(query, cancellationToken);
    }

    public Task<ClientDto?> GetClientAsync(string ip, CancellationToken cancellationToken)
    {
        return clientsService.GetAsync(ip, cancellationToken);
    }

    public Task<ClientAbuseDto?> GetClientAbuseAsync(string ip, CancellationToken cancellationToken)
    {
        return clientsService.GetAbuseAsync(ip, cancellationToken);
    }

    public Task<BlockedContactsPageDto> GetBlockedContactsAsync(BlockedContactsQuery query, CancellationToken cancellationToken)
    {
        return blockedContactsService.GetPageAsync(query, cancellationToken);
    }

    public Task<ReportsPageDto> GetReportsAsync(ReportsQuery query, CancellationToken cancellationToken)
    {
        return reportsService.GetPageAsync(query, cancellationToken);
    }

    public Task<ReportDetailsDto?> GetReportAsync(long id, CancellationToken cancellationToken)
    {
        return reportsService.GetAsync(id, cancellationToken);
    }

    public async Task<GeneratorsViewDto> GetGeneratorsAsync(CancellationToken cancellationToken)
    {
        var overview = await generatorsService.GetOverviewAsync(cancellationToken);
        return new GeneratorsViewDto(
            overview.Configured,
            overview.Generators,
            overview.Totals,
            overview.OldestQueuedAt,
            [.. overview.Generating.Select(ToRow)],
            [.. overview.Queued.Select(ToRow)],
            overview.TotalSlots,
            overview.BusySlots,
            overview.AsOf);
    }

    public Task<SettingsDto> GetSettingsAsync(CancellationToken cancellationToken)
    {
        return settingsService.GetAsync(cancellationToken);
    }

    private static PagedDto<TRow> Rows<TItem, TRow>(PagedDto<TItem> page, Func<TItem, TRow> map)
    {
        return new PagedDto<TRow>([.. page.Items.Select(map)], page.Total, page.Page, page.PageSize);
    }

    private static EntryRowDto ToRow(Entry entry)
    {
        return new EntryRowDto(
            entry.Id,
            entry.State,
            entry.Method,
            entry.Host,
            entry.Target,
            entry.ResponseStatus,
            entry.BodyLength,
            entry.Hits,
            entry.Attempts,
            entry.Generator,
            entry.Error,
            entry.CreatedAt,
            entry.ClaimedAt,
            entry.RequestedAt,
            entry.LastHitAt);
    }

    private static RequestRowDto ToRow(RequestRecord request)
    {
        return new RequestRowDto(
            request.Id,
            request.ReceivedAt,
            request.ClientIp,
            request.Method,
            request.Host,
            request.Target,
            request.EntryId,
            request.Outcome,
            request.ResponseStatus,
            request.DurationMs,
            request.UserAgent);
    }
}