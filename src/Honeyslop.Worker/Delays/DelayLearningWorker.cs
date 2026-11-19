using Honeyslop.Data.Contracts;
using Honeyslop.Data.Delays;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Providers;
using Honeyslop.Data.Repositories;
using Honeyslop.Data.Requests;
using Honeyslop.Worker.Monitoring;
using Microsoft.Extensions.Options;

namespace Honeyslop.Worker.Delays;

internal sealed partial class DelayLearningWorker(
    IServiceScopeFactory scopeFactory,
    ISettingsProvider settingsProvider,
    IOptions<WorkerOptions> options,
    TimeProvider timeProvider,
    ILogger<DelayLearningWorker> logger) : BackgroundService
{
    private const int Page = 500;
    private static readonly TimeSpan Idle = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Unfinished = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CountEvery = TimeSpan.FromSeconds(10);

    private long? _countedAt;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
                LogFailed(logger, exception);
            }

            if (!worked)
                await Task.Delay(Idle, timeProvider, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
        }
    }

    public async Task<bool> RunOnceAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsProvider.GetAsync(cancellationToken);
        if (!settings.AutoDelayEnabled)
            return false;

        await using var scope = scopeFactory.CreateAsyncScope();
        var jobs = scope.ServiceProvider.GetRequiredService<IDelayLearningJobRepository>();
        var now = await scope.ServiceProvider.GetRequiredService<IServerTimeRepository>().GetNowAsync(cancellationToken);
        var instance = options.Value.InstanceName;
        var job = await jobs.TryClaimNextAsync(instance, now, now - Lease, cancellationToken);
        if (job is null)
        {
            WorkerMetrics.DelayJobsWaiting.Set(0);
            return false;
        }

        if (_countedAt is not { } countedAt || timeProvider.GetElapsedTime(countedAt) >= CountEvery)
        {
            WorkerMetrics.DelayJobsWaiting.Set(await jobs.CountWaitingAsync(cancellationToken));
            _countedAt = timeProvider.GetTimestamp();
        }

        var learned = await LearnAsync(scope.ServiceProvider, job.ClientIp, settings, now, cancellationToken);
        WorkerMetrics.DelayJobs.WithLabels(learned ? "learned" : "lost").Inc();
        if (!learned)
            LogLost(logger, job.ClientIp);
        await jobs.CompleteAsync(job.Id, instance, cancellationToken);
        return true;
    }

    private static async Task<bool> LearnAsync(IServiceProvider services, string clientIp, HoneyslopSettings settings, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var probes = services.GetRequiredService<IClientProbeRepository>();
        var requests = services.GetRequiredService<IRequestRepository>();
        var spellings = ClientAddress.Spellings(clientIp);
        var (min, max) = (settings.AutoDelayMinMilliseconds, settings.AutoDelayMaxMilliseconds);
        var current = await probes.GetOrDefaultAsync(clientIp, cancellationToken);
        while (true)
        {
            var timings = await requests.GetTimingsAsync(spellings, current?.LastRequestId ?? 0, Page, cancellationToken);
            var next = current ?? DelayLearner.Start(clientIp, min, max, now);
            var observed = 0;
            var unfinished = false;
            foreach (var request in timings)
            {
                if (request.Outcome == RequestOutcome.Waiting)
                {
                    if (request.ReceivedAt > now - Unfinished)
                    {
                        unfinished = true;
                        break;
                    }
                }
                else
                {
                    next = DelayLearner.Observe(next, request.Outcome != RequestOutcome.Abandoned, request.DurationMs, min, max, now);
                    observed++;
                }

                next.LastRequestId = request.Id;
            }

            if (next.LastRequestId == (current?.LastRequestId ?? 0))
                return true;
            if (current is null ? !await probes.TryAddAsync(next, cancellationToken) : !await probes.TryReplaceAsync(next, current.Version, cancellationToken))
                return false;

            WorkerMetrics.DelayObservations.Inc(observed);
            if (unfinished || timings.Length < Page)
                return true;
            next.Version = current is null ? next.Version : current.Version + 1;
            current = next;
        }
    }

    [LoggerMessage(LogLevel.Warning, "Learning response delays failed, retrying")]
    static partial void LogFailed(ILogger<DelayLearningWorker> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Lost the learned delay of {clientIp} to another worker that took the client over")]
    static partial void LogLost(ILogger<DelayLearningWorker> logger, string clientIp);
}