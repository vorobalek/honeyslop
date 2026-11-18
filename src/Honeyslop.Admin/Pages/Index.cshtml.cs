using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Enums;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Honeyslop.Admin.Pages;

public sealed class IndexModel(IEntriesService entriesService) : SortedListModel<EntryColumn>
{
    [BindProperty(SupportsGet = true)]
    public string? Host { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Target { get; set; }

    [BindProperty(SupportsGet = true)]
    public EntryState? State { get; set; }

    [BindProperty(SupportsGet = true, Name = "p")]
    public int PageNumber { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Expired { get; set; }

    public EntriesPageDto View { get; private set; } = null!;
    public Entry[] Entries => View.Entries.Items;
    public int TotalCount => View.Entries.Total;
    public EntryTotals Statistics => View.Totals;
    public int PageCount => View.Entries.PageCount;
    public EntryFilter Filter => new(Host, Target, State);
    public DateTimeOffset RenderedAt => View.AsOf;

    public Task OnGetAsync(CancellationToken cancellationToken)
    {
        return LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnGetPanelAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        return Partial("_EntriesPanel", this);
    }

    public async Task<IActionResult> OnPostExpireSelectedAsync(long[] ids, CancellationToken cancellationToken)
    {
        var expired = await entriesService.ExpireAsync(ids, cancellationToken);
        return RedirectToSelf(expired);
    }

    public async Task<IActionResult> OnPostExpireFilteredAsync(CancellationToken cancellationToken)
    {
        if (Filter.IsEmpty)
            return BadRequest();
        var expired = await entriesService.ExpireMatchingAsync(Filter, cancellationToken);
        return RedirectToSelf(expired, 0);
    }

    public async Task<IActionResult> OnPostExpireAllAsync(CancellationToken cancellationToken)
    {
        var expired = await entriesService.ExpireMatchingAsync(new EntryFilter(), cancellationToken);
        return RedirectToPage(new
        {
            Expired = expired
        });
    }

    public override string ListPage => "/Index";

    protected override Dictionary<string, string> FilterValues()
    {
        var values = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(Host))
            values[nameof(Host)] = Host;
        if (!string.IsNullOrWhiteSpace(Target))
            values[nameof(Target)] = Target;
        if (State is { } state)
            values[nameof(State)] = state.ToString();
        return values;
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        View = await entriesService.GetPageAsync(new EntriesQuery(Filter, Sorting, PageNumber), cancellationToken);
        PageNumber = View.Entries.Page;
    }

    private RedirectToPageResult RedirectToSelf(int expired, int? pageNumber = null)
    {
        var routeValues = new RouteValueDictionary();
        foreach (var (key, value) in RouteValues(pageNumber ?? PageNumber))
            routeValues[key] = value;
        routeValues[nameof(Expired)] = expired;
        return RedirectToPage(routeValues);
    }
}
