using Honeyslop.Data.Entities;
using Honeyslop.Data.Repositories;
using Honeyslop.Trap.Options;
using Honeyslop.Trap.Settings;
using Microsoft.Extensions.Options;

namespace Honeyslop.Trap.Delays;

internal sealed partial class DelayLearningWorker(
    IServiceScopeFactory scopeFactory,
    ISettingsProvider settingsProvider,
    IOptions<TrapOptions> options,
    TimeProvider timeProvider,
    ILogger<DelayLearningWorker> logger) : BackgroundService
{
    private const int Attempts = 5;
    private static readonly TimeSpan Idle = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(1);

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
        var jobs = scope.ServiceProvider.GetRequiredService<IDelayJobRepository>();
        var now = await scope.ServiceProvider.GetRequiredService<IServerTimeRepository>().GetNowAsync(cancellationToken);
        var instance = options.Value.InstanceName;
        var job = await jobs.TryClaimNextAsync(instance, now, now - Lease, cancellationToken);
        if (job is null)
            return false;

        await LearnAsync(scope.ServiceProvider.GetRequiredService<IClientProbeRepository>(), job, settings.AutoDelayMinMilliseconds, settings.AutoDelayMaxMilliseconds, now, cancellationToken);
        await jobs.CompleteAsync(job.Id, instance, cancellationToken);
        return true;
    }

    private async Task LearnAsync(IClientProbeRepository probes, DelayJob job, int min, int max, DateTimeOffset now, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            var current = await probes.GetOrDefaultAsync(job.ClientIp, cancellationToken);
            var next = DelayLearner.Observe(current ?? DelayLearner.Start(job.ClientIp, min, max, now), job.Answered, job.DurationMs, min, max, now);
            var saved = current is null
                ? await probes.TryAddAsync(next, cancellationToken)
                : await probes.TryReplaceAsync(next, current.Version, cancellationToken);
            if (saved)
                return;
        }

        LogContended(logger, job.ClientIp);
    }

    [LoggerMessage(LogLevel.Warning, "Learning response delays failed, retrying")]
    static partial void LogFailed(ILogger<DelayLearningWorker> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Gave up on a delay observation for {clientIp}: other workers kept changing it")]
    static partial void LogContended(ILogger<DelayLearningWorker> logger, string clientIp);
}