using System.Globalization;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Enums;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Data.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Honeyslop.Admin.Pages;

public sealed class ClientsModel(IClientsService clientsService) : SortedListModel<ClientColumn>
{
    [BindProperty(SupportsGet = true)]
    public int Days { get; set; } = 7;

    [BindProperty(SupportsGet = true, Name = "p")]
    public int PageNumber { get; set; }

    public ClientsPageDto View { get; private set; } = null!;
    public ClientActivity[] Clients => View.Clients.Items;
    public IReadOnlyDictionary<string, CountryDto> Countries => View.Countries;
    public int TotalCount => View.Clients.Total;
    public int PageCount => View.Clients.PageCount;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        View = await clientsService.GetPageAsync(new ClientsQuery(Days, Sorting, PageNumber), cancellationToken);
        PageNumber = View.Clients.Page;
    }

    public override string ListPage => "/Clients";

    protected override Dictionary<string, string> FilterValues()
    {
        return new Dictionary<string, string> { [nameof(Days)] = Days.ToString(CultureInfo.InvariantCulture) };
    }
}
