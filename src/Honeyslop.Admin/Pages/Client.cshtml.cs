using System.Globalization;
using System.Net;
using System.Text.Json;
using Honeyslop.Admin.Clients;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Honeyslop.Admin.Pages;

public sealed partial class ClientModel(
    IRequestRepository requestRepository,
    IEntryRepository entryRepository,
    ICountryLookup countryLookup,
    IWhoisLookup whoisLookup,
    TimeProvider timeProvider,
    ILogger<ClientModel> logger) : PageModel, IRequestList
{
    public const int PageSize = 100;

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

    public ClientSummary? Summary { get; private set; }
    public Country? Country { get; private set; }
    public WhoisInfo? Whois { get; private set; }
    public AbuseReport? Report { get; private set; }
    public RequestSummary[] Requests { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int PageCount => Math.Max(1, (TotalCount + PageSize - 1) / PageSize);
    public DateTimeOffset RenderedAt { get; private set; }
    public string ListPage => "/Client";
    public RequestFilter Filter => new(Ip, Host, Target, Outcome);

    public RequestFilterForm FilterForm =>
        new(Ip, true, Host, Target, Outcome, null, Url.Page("/Client", new { ip = Ip }) ?? string.Empty);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await FindAsync(cancellationToken))
            return NotFound();
        Country = countryLookup.Find(Ip);
        await LoadRequestsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnGetPanelAsync(CancellationToken cancellationToken)
    {
        if (!await FindAsync(cancellationToken))
            return NotFound();
        await LoadRequestsAsync(cancellationToken);
        return Partial("_RequestsPanel", this);
    }

    public async Task<IActionResult> OnGetWhoisAsync(CancellationToken cancellationToken)
    {
        if (!await FindAsync(cancellationToken))
            return NotFound();
        try
        {
            Whois = await whoisLookup.LookupAsync(Ip, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException &&
                                          !cancellationToken.IsCancellationRequested)
        {
            LogLookupFailed(logger, Ip, exception);
        }

        if (Summary is not null)
        {
            var latest = await requestRepository.GetPageAsync(new RequestFilter(Ip), 0, AbuseReport.LatestRequests, cancellationToken);
            Report = AbuseReport.Create(Summary, latest, Whois);
        }

        return Partial("_WhoisPanel", this);
    }

    public Dictionary<string, string> RouteValues(int pageNumber)
    {
        var values = new Dictionary<string, string> { ["ip"] = Ip };
        if (!string.IsNullOrWhiteSpace(Host))
            values[nameof(Host)] = Host;
        if (!string.IsNullOrWhiteSpace(Target))
            values[nameof(Target)] = Target;
        if (Outcome is { } outcome)
            values[nameof(Outcome)] = outcome.ToString();
        if (pageNumber > 0)
            values["p"] = pageNumber.ToString(CultureInfo.InvariantCulture);
        return values;
    }

    private async Task<bool> FindAsync(CancellationToken cancellationToken)
    {
        if (!IPAddress.TryParse(Ip, out _))
            return false;
        Summary = await requestRepository.GetClientOrDefaultAsync(Ip, cancellationToken);
        return Summary is not null || await entryRepository.AnyFromClientAsync(Ip, cancellationToken);
    }

    private async Task LoadRequestsAsync(CancellationToken cancellationToken)
    {
        PageNumber = Math.Max(0, PageNumber);
        RenderedAt = timeProvider.GetUtcNow();
        TotalCount = await requestRepository.CountAsync(Filter, cancellationToken);
        Requests = await requestRepository.GetPageAsync(Filter, PageNumber, PageSize, cancellationToken);
    }

    [LoggerMessage(LogLevel.Warning, "WHOIS lookup of {clientIp} failed")]
    static partial void LogLookupFailed(ILogger<ClientModel> logger, string clientIp, Exception exception);
}
