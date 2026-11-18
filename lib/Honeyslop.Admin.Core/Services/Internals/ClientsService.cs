using System.Net;
using System.Text.Json;
using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Clients;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Enums;
using Honeyslop.Admin.Core.Extensions;
using Honeyslop.Admin.Core.Options;
using Honeyslop.Admin.Core.Text;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Reports;
using Honeyslop.Reports.Contracts;
using Microsoft.Extensions.Options;

namespace Honeyslop.Admin.Core.Services.Internals;

internal sealed partial class ClientsService(
    IRequestRepository requestRepository,
    IEntryRepository entryRepository,
    IAbuseReportRepository abuseReportRepository,
    ISettingsRepository settingsRepository,
    IRequestArchiveBuilder archiveBuilder,
    ICountryLookup countryLookup,
    IWhoisLookup whoisLookup,
    IOptions<AbuseReportOptions> reportOptions,
    IAccessGuard access,
    IServerTimeRepository serverTime,
    ILogger<ClientsService> logger) : IClientsService
{
    public async Task<ClientsPageDto> GetPageAsync(ClientsQuery query, CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        var page = Math.Max(0, query.Page);
        var asOf = await serverTime.GetNowAsync(cancellationToken);
        var since = query.Days > 0 ? asOf.AddDays(-query.Days) : DateTimeOffset.MinValue;
        var total = await requestRepository.CountClientsAsync(since, cancellationToken);
        var (order, direction) = query.Sort.ToOrder();
        var clients = await requestRepository.GetClientsAsync(since, order, direction, page, IClientsService.PageSize, cancellationToken);
        return new ClientsPageDto(
            new PagedDto<ClientActivity>(clients, total, page, IClientsService.PageSize),
            countryLookup.FindAll(clients.Select(x => x.ClientIp)),
            asOf);
    }

    public async Task<ClientDto?> GetAsync(string ip, CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        return await FindAsync(ip, cancellationToken) is { } known
            ? new ClientDto(ip, countryLookup.Find(ip), known.Summary)
            : null;
    }

    public async Task<ClientAbuseDto?> GetAbuseAsync(string ip, CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        if (await FindAsync(ip, cancellationToken) is not { Summary: var summary })
            return null;
        var whois = await TryLookupAsync(ip, cancellationToken);
        var (reports, newSinceLastSent) = await LoadReportsAsync(ip, cancellationToken);
        AbuseDraftDto? draft = null;
        if (access.Viewer.CanOperate && summary is not null)
        {
            var archived = Math.Min(summary.Requests, AbuseLetters.MaxAttachedRequests);
            draft = new AbuseDraftDto(
                await WriteLetterAsync(summary, whois, cancellationToken),
                await DefaultSenderNameAsync(cancellationToken),
                reportOptions.Value.FromAddress,
                archived,
                RequestLogArchive.FileName(ip));
        }

        return new ClientAbuseDto(ip, whois, [.. reports.Select(x => x.ToRow(access.Viewer))], newSinceLastSent, draft);
    }

    public async Task<RequestLogArchive?> GetArchiveAsync(string ip, CancellationToken cancellationToken)
    {
        access.EnsureCanOperate();
        if (await FindAsync(ip, cancellationToken) is null)
            return null;
        return await archiveBuilder.BuildAsync(ip, AbuseLetters.MaxAttachedRequests, cancellationToken);
    }

    public async Task<ReportRequestDto> ReportAsync(string ip, string? senderName, CancellationToken cancellationToken)
    {
        access.EnsureCanOperate();
        if (await FindAsync(ip, cancellationToken) is not { Summary: { } summary })
            return new ReportRequestDto(ReportOutcome.NotFound);

        var (reports, newSinceLastSent) = await LoadReportsAsync(ip, cancellationToken);
        if (reports.FirstOrDefault(x => x.Status == AbuseReportStatus.Pending) is { } pending)
            return new ReportRequestDto(ReportOutcome.AlreadyQueued, pending.Id);
        if (reports.Any(x => x.Status == AbuseReportStatus.Sent) && newSinceLastSent == 0)
            return new ReportRequestDto(ReportOutcome.AlreadyReported);

        var whois = await TryLookupAsync(ip, cancellationToken);
        if (whois is not { AbuseContacts.Length: > 0 })
            return new ReportRequestDto(ReportOutcome.NoContact);

        var letter = await WriteLetterAsync(summary, whois, cancellationToken);
        var now = await serverTime.GetNowAsync(cancellationToken);
        var report = new AbuseReport
        {
            ClientIp = ip,
            Recipients = whois.AbuseContacts,
            Subject = letter.Subject,
            Body = letter.Body,
            RequestedBy = access.Viewer.Name ?? "admin",
            SenderName = SenderName.Clean(senderName) ?? await DefaultSenderNameAsync(cancellationToken),
            Status = AbuseReportStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
        return await abuseReportRepository.TryAddAsync(report, cancellationToken)
            ? new ReportRequestDto(ReportOutcome.Queued, report.Id)
            : new ReportRequestDto(ReportOutcome.AlreadyQueued);
    }

    private async Task<KnownClient?> FindAsync(string ip, CancellationToken cancellationToken)
    {
        if (!IPAddress.TryParse(ip, out _))
            return null;
        var summary = await requestRepository.GetClientOrDefaultAsync(ip, cancellationToken);
        return summary is not null || await entryRepository.AnyFromClientAsync(ip, cancellationToken)
            ? new KnownClient(summary)
            : null;
    }

    private async Task<(AbuseReport[] Reports, int NewSinceLastSent)> LoadReportsAsync(string ip, CancellationToken cancellationToken)
    {
        var reports = await abuseReportRepository.GetByClientAsync(ip, cancellationToken);
        var lastSent = reports.FirstOrDefault(x => x.Status == AbuseReportStatus.Sent);
        var newSinceLastSent = lastSent?.LastRequestId is { } lastRequestId
            ? await requestRepository.CountAsync(new RequestFilter(ip, AfterId: lastRequestId), cancellationToken)
            : 0;
        return (reports, newSinceLastSent);
    }

    private async Task<WhoisDto?> TryLookupAsync(string ip, CancellationToken cancellationToken)
    {
        try
        {
            return await whoisLookup.LookupAsync(ip, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException &&
                                          !cancellationToken.IsCancellationRequested)
        {
            LogLookupFailed(logger, ip, exception);
            return null;
        }
    }

    private async Task<LetterDto> WriteLetterAsync(ClientActivity summary, WhoisDto? whois, CancellationToken cancellationToken)
    {
        var latest = await requestRepository.GetPageAsync(new RequestFilter(summary.ClientIp), null, 0, AbuseLetters.LatestRequests, cancellationToken);
        return AbuseLetters.Write(summary, latest, whois);
    }

    private async Task<string?> DefaultSenderNameAsync(CancellationToken cancellationToken)
    {
        var personal = access.Viewer.UserId is { } userId
            ? (await settingsRepository.GetAsync(userId, cancellationToken)).User?.SenderName
            : null;
        return personal ?? reportOptions.Value.DefaultSenderName;
    }

    private sealed record KnownClient(ClientActivity? Summary);

    [LoggerMessage(LogLevel.Warning, "WHOIS lookup of {clientIp} failed")]
    static partial void LogLookupFailed(ILogger<ClientsService> logger, string clientIp, Exception exception);
}
