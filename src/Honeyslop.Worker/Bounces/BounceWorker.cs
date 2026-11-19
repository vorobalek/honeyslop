using System.Globalization;
using Honeyslop.Data.Repositories;
using Honeyslop.Worker.Mail;
using Honeyslop.Worker.Monitoring;
using Microsoft.Extensions.Options;

namespace Honeyslop.Worker.Bounces;

internal sealed partial class BounceWorker(
    IServiceScopeFactory scopeFactory,
    IMailbox mailbox,
    IOptions<ImapOptions> imapOptions,
    IOptions<BounceOptions> options,
    TimeProvider timeProvider,
    ILogger<BounceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configured = imapOptions.Value.Configured;
        WorkerMetrics.MailboxConfigured.Set(configured ? 1 : 0);
        if (!configured)
        {
            LogNotConfigured(logger);
            return;
        }

        LogStarted(logger, string.Join(", ", options.Value.Folders));
        while (!stoppingToken.IsCancellationRequested)
        {
            var read = true;
            foreach (var folder in options.Value.Folders)
            {
                try
                {
                    await ScanAsync(folder, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    read = false;
                    LogScanFailed(logger, folder, exception);
                }
            }

            if (read)
                WorkerMetrics.MailboxRead.Set(timeProvider.GetUtcNow().ToUnixTimeMilliseconds() / 1000.0);

            await Task.Delay(options.Value.PollInterval, timeProvider, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
        }
    }

    public async Task ScanAsync(string folder, CancellationToken cancellationToken)
    {
        while (true)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var cursors = scope.ServiceProvider.GetRequiredService<IMailboxCursorRepository>();
            var cursor = await cursors.GetOrDefaultAsync(folder, cancellationToken);
            var batch = await mailbox.ReadAsync(folder, cursor?.UidValidity, cursor?.LastUid ?? 0, options.Value.BatchSize, cancellationToken);
            if (cursor is not null && cursor.UidValidity == batch.UidValidity && cursor.LastUid == batch.LastUid)
                return;

            var now = await scope.ServiceProvider.GetRequiredService<IServerTimeRepository>().GetNowAsync(cancellationToken);
            var notices = batch.Messages
                .Select(x => (x.Uid, Notice: BounceNotice.Parse(x.Message)))
                .Where(x => x.Notice is not null)
                .Select(x => (x.Uid, Notice: x.Notice!))
                .ToArray();
            string[] referenced = [.. notices.SelectMany(x => x.Notice.LookupIds).Distinct()];
            if (referenced.Length > 0)
            {
                var reports = await scope.ServiceProvider.GetRequiredService<IAbuseReportRepository>().GetByMessageIdsAsync(referenced, cancellationToken);
                var bounces = notices
                    .SelectMany(x => x.Notice.ToBounces(
                        string.Create(CultureInfo.InvariantCulture, $"{folder}/{batch.UidValidity}/{x.Uid}"),
                        reports,
                        now))
                    .ToArray();
                if (bounces.Length > 0)
                {
                    await scope.ServiceProvider.GetRequiredService<IAbuseReportBounceRepository>().AddAsync(bounces, cancellationToken);
                    foreach (var bounce in bounces)
                        LogBounced(logger, bounce.ReportId, bounce.Recipient, bounce.Status, bounce.Diagnostic);
                }
            }

            await cursors.SaveAsync(folder, batch.UidValidity, batch.LastUid, now, cancellationToken);
            if (!batch.HasMore)
                return;
        }
    }

    [LoggerMessage(LogLevel.Information, "IMAP is not configured: the worker does not track bounces")]
    static partial void LogNotConfigured(ILogger<BounceWorker> logger);

    [LoggerMessage(LogLevel.Information, "Tracking bounces in {folders}")]
    static partial void LogStarted(ILogger<BounceWorker> logger, string folders);

    [LoggerMessage(LogLevel.Information, "Abuse report {id} bounced from {recipient}: {status} {diagnostic}")]
    static partial void LogBounced(ILogger<BounceWorker> logger, long id, string recipient, string? status, string? diagnostic);

    [LoggerMessage(LogLevel.Warning, "Could not read bounces from {folder}")]
    static partial void LogScanFailed(ILogger<BounceWorker> logger, string folder, Exception exception);
}