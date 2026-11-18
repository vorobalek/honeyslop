using System.Globalization;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Honeyslop.Admin.Pages;

public sealed class RequestsModel(
    IRequestRepository requestRepository,
    TimeProvider timeProvider) : SortedListModel<RequestColumn>, IRequestList
{
    public const int PageSize = 100;

    [BindProperty(SupportsGet = true)]
    public string? ClientIp { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Host { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Target { get; set; }

    [BindProperty(SupportsGet = true)]
    public RequestOutcome? Outcome { get; set; }

    [BindProperty(SupportsGet = true)]
    public long? EntryId { get; set; }

    [BindProperty(SupportsGet = true, Name = "p")]
    public int PageNumber { get; set; }

    public RequestSummary[] Requests { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int PageCount => Math.Max(1, (TotalCount + PageSize - 1) / PageSize);
    public DateTimeOffset RenderedAt { get; private set; }
    public override string ListPage => "/Requests";
    public RequestFilter Filter => new(ClientIp, Host, Target, Outcome, EntryId);

    public RequestFilterForm FilterForm =>
        new(ClientIp, false, Host, Target, Outcome, EntryId, Url.Page("/Requests") ?? string.Empty);

    public Task OnGetAsync(CancellationToken cancellationToken)
    {
        return LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnGetPanelAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        return Partial("_RequestsPanel", this);
    }

    protected override Dictionary<string, string> FilterValues()
    {
        var values = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(ClientIp))
            values[nameof(ClientIp)] = ClientIp;
        if (!string.IsNullOrWhiteSpace(Host))
            values[nameof(Host)] = Host;
        if (!string.IsNullOrWhiteSpace(Target))
            values[nameof(Target)] = Target;
        if (Outcome is { } outcome)
            values[nameof(Outcome)] = outcome.ToString();
        if (EntryId is { } entryId)
            values[nameof(EntryId)] = entryId.ToString(CultureInfo.InvariantCulture);
        return values;
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        PageNumber = Math.Max(0, PageNumber);
        RenderedAt = timeProvider.GetUtcNow();
        TotalCount = await requestRepository.CountAsync(Filter, cancellationToken);
        Requests = await requestRepository.GetPageAsync(Filter, Sorting, PageNumber, PageSize, cancellationToken);
    }
}
