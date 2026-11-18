using System.Globalization;
using System.Net;
using System.Text.Json;
using Honeyslop.Admin.Access;
using Honeyslop.Admin.Clients;
using Honeyslop.Admin.Extensions;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using AbuseReportEntity = Honeyslop.Data.Entities.AbuseReport;

namespace Honeyslop.Admin.Pages;

public sealed partial class ClientModel(
    IRequestRepository requestRepository,
    IEntryRepository entryRepository,
    IAbuseReportRepository abuseReportRepository,
    IRequestArchiveBuilder archiveBuilder,
    ICountryLookup countryLookup,
    IWhoisLookup whoisLookup,
    IUserSettingsRepository userSettingsRepository,
    IOptions<AbuseReportOptions> reportOptions,
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

    [BindProperty(SupportsGet = true)]
    public ReportNotice? Report { get; set; }

    [BindProperty]
    public string? SenderName { get; set; }

    public ClientSummary? Summary { get; private set; }
    public Country? Country { get; private set; }
    public WhoisInfo? Whois { get; private set; }
    public AbuseLetter? Letter { get; private set; }
    public AbuseReportEntity[] Reports { get; private set; } = [];
    public RequestSummary[] Requests { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int PageCount => Math.Max(1, (TotalCount + PageSize - 1) / PageSize);
    public DateTimeOffset RenderedAt { get; private set; }
    public string ListPage => "/Client";
    public RequestFilter Filter => new(Ip, Host, Target, Outcome);
    public int NewSinceLastSent { get; private set; }
    public AbuseReportEntity? LastReport => Reports.FirstOrDefault();
    public AbuseReportEntity? LastSent => Reports.FirstOrDefault(x => x.Status == AbuseReportStatus.Sent);
    public bool ReportPending => Reports.Any(x => x.Status == AbuseReportStatus.Pending);
    public bool AlreadyReported => LastSent is not null && NewSinceLastSent == 0;

    public string SendConfirmation =>
        $"Send the abuse report about {Ip} to {string.Join(", ", Whois?.AbuseContacts ?? [])}?";

    public string? FromAddress => reportOptions.Value.FromAddress;

    public int ArchivedRequests => Math.Min(Summary?.Requests ?? 0, AbuseLetter.MaxAttachedRequests);

    public string SendConfirmationDetail =>
        $"It goes out right away with {ArchivedRequests.Of("request")} from this address attached, and cannot be taken back.";

    public string ArchiveDescription =>
        $"{RequestLogArchive.FileName(Ip)}: {ArchivedRequests.Of("request")} from this address as received, oldest first";

    public string SendLabel
    {
        get
        {
            var to = string.Join(", ", Whois?.AbuseContacts ?? []);
            return LastSent is null
                ? $"Send to {to}"
                : $"Send a follow-up to {to} ({NewSinceLastSent.Of("new request")})";
        }
    }

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
        Whois = await TryLookupAsync(cancellationToken);
        await LoadReportsAsync(cancellationToken);
        if (User.CanOperate && Summary is not null)
        {
            SenderName = await DefaultSenderNameAsync(cancellationToken);
            Letter = await WriteLetterAsync(Summary, cancellationToken);
        }
        return Partial("_WhoisPanel", this);
    }

    public async Task<IActionResult> OnGetArchiveAsync(CancellationToken cancellationToken)
    {
        if (!User.CanOperate)
            return Challenge();
        if (!await FindAsync(cancellationToken))
            return NotFound();
        var archive = await archiveBuilder.BuildAsync(Ip, AbuseLetter.MaxAttachedRequests, cancellationToken);
        return archive is null
            ? NotFound()
            : File(archive.Content, "application/zip", RequestLogArchive.FileName(Ip));
    }

    public async Task<IActionResult> OnPostReportAsync(CancellationToken cancellationToken)
    {
        if (!await FindAsync(cancellationToken) || Summary is null)
            return NotFound();

        await LoadReportsAsync(cancellationToken);
        if (ReportPending)
            return RedirectToClient(ReportNotice.AlreadyQueued);
        if (AlreadyReported)
            return RedirectToClient(ReportNotice.AlreadyReported);

        Whois = await TryLookupAsync(cancellationToken);
        if (Whois is not { AbuseContacts.Length: > 0 } whois)
            return RedirectToClient(ReportNotice.NoContact);

        SenderName = Clients.SenderName.Clean(SenderName) ?? await DefaultSenderNameAsync(cancellationToken);
        var letter = await WriteLetterAsync(Summary, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var queued = await abuseReportRepository.TryAddAsync(
            new AbuseReportEntity
            {
                ClientIp = Ip,
                Recipients = whois.AbuseContacts,
                Subject = letter.Subject,
                Body = letter.Body,
                RequestedBy = User.DisplayName ?? "admin",
                SenderName = SenderName,
                Status = AbuseReportStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now
            },
            cancellationToken);
        return RedirectToClient(queued ? ReportNotice.Queued : ReportNotice.AlreadyQueued);
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

    private async Task<WhoisInfo?> TryLookupAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await whoisLookup.LookupAsync(Ip, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException &&
                                          !cancellationToken.IsCancellationRequested)
        {
            LogLookupFailed(logger, Ip, exception);
            return null;
        }
    }

    private async Task<AbuseLetter> WriteLetterAsync(ClientSummary summary, CancellationToken cancellationToken)
    {
        var latest = await requestRepository.GetPageAsync(new RequestFilter(Ip), 0, AbuseLetter.LatestRequests, cancellationToken);
        return AbuseLetter.Create(summary, latest, Whois);
    }

    private async Task<string?> DefaultSenderNameAsync(CancellationToken cancellationToken)
    {
        var personal = User.UserId is { } userId
            ? (await userSettingsRepository.GetAsync(userId, cancellationToken)).SenderName
            : null;
        return personal ?? reportOptions.Value.DefaultSenderName;
    }

    private async Task LoadReportsAsync(CancellationToken cancellationToken)
    {
        Reports = await abuseReportRepository.GetByClientAsync(Ip, cancellationToken);
        NewSinceLastSent = LastSent?.LastRequestId is { } lastRequestId
            ? await requestRepository.CountAsync(new RequestFilter(Ip, AfterId: lastRequestId), cancellationToken)
            : 0;
    }

    private RedirectToPageResult RedirectToClient(ReportNotice notice)
    {
        return RedirectToPage("/Client", new { ip = Ip, Report = notice });
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

    public enum ReportNotice
    {
        Queued,
        AlreadyQueued,
        AlreadyReported,
        NoContact
    }
}
