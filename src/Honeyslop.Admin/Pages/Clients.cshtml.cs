using Honeyslop.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Honeyslop.Admin.Pages;

public sealed class ClientsModel(
    IRequestRepository requestRepository,
    TimeProvider timeProvider) : SortedListModel<ClientColumn>
{
    public const int PageSize = 50;

    [BindProperty(SupportsGet = true)]
    public int Days { get; set; } = 7;

    [BindProperty(SupportsGet = true, Name = "p")]
    public int PageNumber { get; set; }

    public ClientSummary[] Clients { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int PageCount => Math.Max(1, (TotalCount + PageSize - 1) / PageSize);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        PageNumber = Math.Max(0, PageNumber);
        var since = Days > 0
            ? timeProvider.GetUtcNow().AddDays(-Days)
            : DateTimeOffset.MinValue;
        TotalCount = await requestRepository.CountClientsAsync(since, cancellationToken);
        Clients = await requestRepository.GetClientsAsync(since, Sorting, PageNumber, PageSize, cancellationToken);
    }

    public override string ListPage => "/Clients";

    protected override Dictionary<string, string> FilterValues()
    {
        return new Dictionary<string, string> { [nameof(Days)] = Days.ToString(System.Globalization.CultureInfo.InvariantCulture) };
    }
}
