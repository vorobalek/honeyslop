using Honeyslop.Admin.Access;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Enums;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Admin.Core.Text;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Reports.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Honeyslop.Admin.Pages;

public sealed class ClientModel(
    IClientsService clientsService,
    IRequestsService requestsService) : SortedListModel<RequestColumn>, IRequestList
{
    [BindProperty(SupportsGet = true)]
    public string Ip { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? Host { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Target { get; set; }

    [BindProperty(SupportsGet = true)]
    public RequestOutcome? Outcome { get; set; }

    [BindProperty(SupportsGet = true, Name = "p")]
    public int PageNumber { get; set; }

    [BindProperty(SupportsGet = true)]
    public ReportOutcome? Report { get; set; }

    [BindProperty]
    public string? SenderName { get; set; }

    public ClientDto Profile { get; private set; } = null!;
    public ClientAbuseDto Abuse { get; private set; } = null!;
    public RequestsPageDto View { get; private set; } = null!;
    public ClientActivity? Summary => Profile.Activity;
    public CountryDto? Country => Profile.Country;
    public RequestRecord[] Requests => View.Requests.Items;
    public IReadOnlyDictionary<string, CountryDto> Countries => View.Countries;
    public int TotalCount => View.Requests.Total;
    public int PageCount => View.Requests.PageCount;
    public DateTimeOffset RenderedAt => View.AsOf;
    public override string ListPage => "/Client";
    public RequestFilter Filter => new(Ip, Host, Target, Outcome);

    public string SendConfirmation =>
        $"Send the abuse report about {Ip} to {string.Join(", ", Abuse.Recipients)}?";

    public string SendConfirmationDetail =>
        $"It goes out right away with {(Abuse.Draft?.ArchivedRequests ?? 0).Of("request")} from this address attached, and cannot be taken back.";

    public string ArchiveDescription =>
        $"{Abuse.Draft?.ArchiveFileName}: {(Abuse.Draft?.ArchivedRequests ?? 0).Of("request")} from this address as received, oldest first";

    public string SendLabel
    {
        get
        {
            var to = string.Join(", ", Abuse.Recipients);
            return Abuse.LastSent is null
                ? $"Send to {to}"
                : $"Send a follow-up to {to} ({Abuse.NewSinceLastSent.Of("new request")})";
        }
    }

    public RequestFilterForm FilterForm =>
        new(Ip, true, Host, Target, Outcome, null, Url.Page("/Client", new { ip = Ip }) ?? string.Empty);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (await clientsService.GetAsync(Ip, cancellationToken) is not { } profile)
            return NotFound();
        Profile = profile;
        await LoadRequestsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnGetPanelAsync(CancellationToken cancellationToken)
    {
        if (await clientsService.GetAsync(Ip, cancellationToken) is not { } profile)
            return NotFound();
        Profile = profile;
        await LoadRequestsAsync(cancellationToken);
        return Partial("_RequestsPanel", this);
    }

    public async Task<IActionResult> OnGetWhoisAsync(CancellationToken cancellationToken)
    {
        if (await clientsService.GetAbuseAsync(Ip, cancellationToken) is not { } abuse)
            return NotFound();
        Abuse = abuse;
        SenderName = abuse.Draft?.SenderName;
        return Partial("_WhoisPanel", this);
    }

    public async Task<IActionResult> OnGetArchiveAsync(CancellationToken cancellationToken)
    {
        if (!User.CanOperate)
            return Challenge();
        var archive = await clientsService.GetArchiveAsync(Ip, cancellationToken);
        return archive is null
            ? NotFound()
            : File(archive.Content, "application/zip", RequestLogArchive.FileName(Ip));
    }

    public async Task<IActionResult> OnPostReportAsync(CancellationToken cancellationToken)
    {
        var request = await clientsService.ReportAsync(Ip, SenderName, cancellationToken);
        return request.Outcome == ReportOutcome.NotFound
            ? NotFound()
            : RedirectToPage("/Client", new { ip = Ip, Report = request.Outcome });
    }

    protected override Dictionary<string, string> FilterValues()
    {
        var values = new Dictionary<string, string> { ["ip"] = Ip };
        if (!string.IsNullOrWhiteSpace(Host))
            values[nameof(Host)] = Host;
        if (!string.IsNullOrWhiteSpace(Target))
            values[nameof(Target)] = Target;
        if (Outcome is { } outcome)
            values[nameof(Outcome)] = outcome.ToString();
        return values;
    }

    private async Task LoadRequestsAsync(CancellationToken cancellationToken)
    {
        View = await requestsService.GetPageAsync(new RequestsQuery(Filter, Sorting, PageNumber), cancellationToken);
        PageNumber = View.Requests.Page;
    }
}
