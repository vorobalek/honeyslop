using System.Globalization;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Enums;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Honeyslop.Admin.Pages;

public sealed class RequestsModel(IRequestsService requestsService) : SortedListModel<RequestColumn>, IRequestList
{
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

    public RequestsPageDto View { get; private set; } = null!;
    public RequestRecord[] Requests => View.Requests.Items;
    public IReadOnlyDictionary<string, CountryDto> Countries => View.Countries;
    public int TotalCount => View.Requests.Total;
    public int PageCount => View.Requests.PageCount;
    public DateTimeOffset RenderedAt => View.AsOf;
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
        View = await requestsService.GetPageAsync(new RequestsQuery(Filter, Sorting, PageNumber), cancellationToken);
        PageNumber = View.Requests.Page;
    }
}
