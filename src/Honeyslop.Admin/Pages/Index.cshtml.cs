using Honeyslop.Data.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Honeyslop.Admin.Pages;

public sealed class IndexModel(
    IEntryRepository entryRepository,
    TimeProvider timeProvider) : SortedListModel<EntryColumn>
{
    public const int PageSize = 50;

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

    public EntrySummary[] Entries { get; private set; } = [];
    public int TotalCount { get; private set; }
    public EntryStatistics Statistics { get; private set; } = new(0, 0, 0, 0, 0, 0, 0);
    public int PageCount => Math.Max(1, (TotalCount + PageSize - 1) / PageSize);
    public EntryFilter Filter => new(Host, Target, State);
    public DateTimeOffset RenderedAt { get; private set; }

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
        var expired = ids.Length > 0
            ? await entryRepository.ExpireAsync(ids, timeProvider.GetUtcNow(), cancellationToken)
            : 0;
        return RedirectToSelf(expired);
    }

    public async Task<IActionResult> OnPostExpireFilteredAsync(CancellationToken cancellationToken)
    {
        if (Filter.IsEmpty)
            return BadRequest();
        var expired = await entryRepository.ExpireAsync(Filter, timeProvider.GetUtcNow(), cancellationToken);
        return RedirectToSelf(expired, 0);
    }

    public async Task<IActionResult> OnPostExpireAllAsync(CancellationToken cancellationToken)
    {
        var expired = await entryRepository.ExpireAsync(new EntryFilter(), timeProvider.GetUtcNow(), cancellationToken);
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
        PageNumber = Math.Max(0, PageNumber);
        RenderedAt = timeProvider.GetUtcNow();
        Statistics = await entryRepository.GetStatisticsAsync(cancellationToken);
        TotalCount = await entryRepository.CountAsync(Filter, cancellationToken);
        Entries = await entryRepository.GetPageAsync(Filter, Sorting, PageNumber, PageSize, cancellationToken);
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
