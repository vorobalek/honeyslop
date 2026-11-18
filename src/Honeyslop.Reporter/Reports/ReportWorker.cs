using Honeyslop.Data.Entities;
using Honeyslop.Data.Repositories;
using Honeyslop.Reporter.Mail;
using Honeyslop.Reporter.Monitoring;
using Honeyslop.Reports;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;

namespace Honeyslop.Reporter.Reports;

internal sealed partial class ReportWorker(
    IServiceScopeFactory scopeFactory,
    IMailSender mailSender,
    IOptions<SmtpOptions> smtpOptions,
    IOptions<ReporterOptions> options,
    TimeProvider timeProvider,
    ILogger<ReportWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configured = smtpOptions.Value.Configured;
        ReporterMetrics.Initialize();
        ReporterMetrics.Configured.Set(configured ? 1 : 0);
        if (!configured)
        {
            LogNotConfigured(logger);
            return;
        }

        LogStarted(logger, options.Value.InstanceName);
        while (!stoppingToken.IsCancellationRequested)
        {
            var worked = false;
            try
            {
                worked = await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                LogLoopFailed(logger, exception);
            }

            if (!worked)
                await Task.Delay(options.Value.PollInterval, timeProvider, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
        }
    }

    private async Task<bool> RunOnceAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow();
        await using var scope = scopeFactory.CreateAsyncScope();
        var reports = scope.ServiceProvider.GetRequiredService<IAbuseReportRepository>();
        if (await reports.FailExhaustedAsync(settings.MaxAttempts, now - settings.LeaseTimeout, now, stoppingToken) is > 0 and var exhausted)
        {
            ReporterMetrics.Reports.WithLabels("failed").Inc(exhausted);
            LogExhausted(logger, exhausted);
        }

        var report = await reports.TryClaimNextAsync(settings.InstanceName, now, now - settings.LeaseTimeout, settings.MaxAttempts, stoppingToken);
        if (report is null)
            return false;

        using var lease = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var heartbeat = KeepClaimAsync(report.Id, lease);
        try
        {
            await SendAsync(scope.ServiceProvider, report, lease.Token);
        }
        finally
        {
            await lease.CancelAsync();
            await heartbeat;
        }

        return true;
    }

    private async Task SendAsync(IServiceProvider services, AbuseReport report, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var reports = services.GetRequiredService<IAbuseReportRepository>();
        var archive = await services
            .GetRequiredService<IRequestArchiveBuilder>()
            .BuildAsync(report.ClientIp, settings.MaxAttachedRequests, cancellationToken);
        if (archive is null)
        {
            await reports.FailAsync(report.Id, settings.InstanceName, "The request history has nothing left from this address", timeProvider.GetUtcNow(), CancellationToken.None);
            ReporterMetrics.Reports.WithLabels("failed").Inc();
            return;
        }

        var sender = new MailboxAddress(report.SenderName ?? string.Empty, smtpOptions.Value.From!);
        var message = ReportMessage.Create(report, sender, archive.Content, timeProvider.GetUtcNow());
        try
        {
            await mailSender.SendAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await HandleFailureAsync(reports, report, exception);
            return;
        }

        await reports.CompleteAsync(report.Id, settings.InstanceName, message.MessageId ?? string.Empty, sender.ToString(), archive.Requests, archive.LastRequestId, timeProvider.GetUtcNow(), CancellationToken.None);
        ReporterMetrics.Reports.WithLabels("sent").Inc();
        LogSent(logger, report.Id, report.ClientIp, string.Join(", ", report.Recipients), archive.Requests);
    }

    private async Task HandleFailureAsync(IAbuseReportRepository reports, AbuseReport report, Exception exception)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow();
        var error = $"{exception.GetType().Name}: {exception.Message}";
        var rejected = exception is SmtpCommandException
        {
            StatusCode: >= (SmtpStatusCode)500,
            ErrorCode: SmtpErrorCode.RecipientNotAccepted or SmtpErrorCode.SenderNotAccepted or SmtpErrorCode.MessageNotAccepted
        };
        if (rejected || report.Attempts >= settings.MaxAttempts)
        {
            await reports.FailAsync(report.Id, settings.InstanceName, error, now, CancellationToken.None);
            ReporterMetrics.Reports.WithLabels("failed").Inc();
            LogFailed(logger, report.Id, report.ClientIp, exception);
            return;
        }

        var delay = settings.RetryDelays[Math.Min(report.Attempts, settings.RetryDelays.Length) - 1];
        await reports.RetryAsync(report.Id, settings.InstanceName, error, now + delay, now, CancellationToken.None);
        ReporterMetrics.Reports.WithLabels("retried").Inc();
        LogRetrying(logger, report.Id, report.ClientIp, delay, exception);
    }

    private async Task KeepClaimAsync(long id, CancellationTokenSource lease)
    {
        var settings = options.Value;
        try
        {
            while (true)
            {
                await Task.Delay(settings.HeartbeatInterval, timeProvider, lease.Token);
                await using var scope = scopeFactory.CreateAsyncScope();
                var kept = await scope.ServiceProvider
                    .GetRequiredService<IAbuseReportRepository>()
                    .HeartbeatAsync(id, settings.InstanceName, timeProvider.GetUtcNow(), lease.Token);
                if (kept)
                    continue;
                LogClaimLost(logger, id);
                await lease.CancelAsync();
                return;
            }
        }
        catch (OperationCanceledException) when (lease.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            LogHeartbeatFailed(logger, id, exception);
        }
    }

    [LoggerMessage(LogLevel.Warning, "SMTP is not configured: the reporter leaves abuse reports in the queue")]
    static partial void LogNotConfigured(ILogger<ReportWorker> logger);

    [LoggerMessage(LogLevel.Information, "Reporter {instance} takes abuse reports from the queue")]
    static partial void LogStarted(ILogger<ReportWorker> logger, string instance);

    [LoggerMessage(LogLevel.Information, "Sent abuse report {id} about {clientIp} to {recipients} with {requests} requests attached")]
    static partial void LogSent(ILogger<ReportWorker> logger, long id, string clientIp, string recipients, int requests);

    [LoggerMessage(LogLevel.Warning, "Abuse report {id} about {clientIp} failed, retrying in {delay}")]
    static partial void LogRetrying(ILogger<ReportWorker> logger, long id, string clientIp, TimeSpan delay, Exception exception);

    [LoggerMessage(LogLevel.Error, "Abuse report {id} about {clientIp} failed for good")]
    static partial void LogFailed(ILogger<ReportWorker> logger, long id, string clientIp, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Gave up on {count} abuse reports whose reporter stopped while sending them")]
    static partial void LogExhausted(ILogger<ReportWorker> logger, int count);

    [LoggerMessage(LogLevel.Warning, "Lost the claim on abuse report {id}, stopping its delivery")]
    static partial void LogClaimLost(ILogger<ReportWorker> logger, long id);

    [LoggerMessage(LogLevel.Warning, "Could not extend the claim on abuse report {id}")]
    static partial void LogHeartbeatFailed(ILogger<ReportWorker> logger, long id, Exception exception);

    [LoggerMessage(LogLevel.Error, "Abuse report queue pass failed")]
    static partial void LogLoopFailed(ILogger<ReportWorker> logger, Exception exception);
}
