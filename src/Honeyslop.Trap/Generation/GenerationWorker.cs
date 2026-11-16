using Honeyslop.Llm;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Trap.Llm;
using Honeyslop.Trap.Monitoring;
using Honeyslop.Trap.Options;
using Honeyslop.Trap.Responses;
using Honeyslop.Trap.Settings;
using Microsoft.Extensions.Options;

namespace Honeyslop.Trap.Generation;

internal sealed partial class GenerationWorker(
    IGenerationSignal generationSignal,
    IEntryWatcher entryWatcher,
    ISettingsProvider settingsProvider,
    IServiceScopeFactory serviceScopeFactory,
    ILlmDispatcher llmDispatcher,
    IPromptBuilder promptBuilder,
    IResponseSanitizer responseSanitizer,
    IOptions<TrapOptions> options,
    TimeProvider timeProvider,
    ILogger<GenerationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan IdleInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan OutputInterval = TimeSpan.FromSeconds(1);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.WhenAll(Enumerable
            .Range(0, Math.Max(1, options.Value.Workers))
            .Select(_ => Task.Run(() => RunAsync(stoppingToken), stoppingToken)));
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
            try
            {
                var (entryId, settings) = await ClaimNextAsync(stoppingToken);
                if (entryId is { } id)
                    entryWatcher.Complete(id, await GenerateAsync(id, settings, stoppingToken));
                else
                    await generationSignal.WaitAsync(IdleInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                LogLoopFailed(logger, exception);
                await generationSignal.WaitAsync(IdleInterval, stoppingToken);
            }
    }

    private async Task<(long? EntryId, HoneyslopSettings Settings)> ClaimNextAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsProvider.GetAsync(cancellationToken);
        if (!settings.GenerationEnabled)
            return (null, settings);

        var now = timeProvider.GetUtcNow();
        await using var asyncScope = serviceScopeFactory.CreateAsyncScope();
        var entryId = await asyncScope
            .ServiceProvider
            .GetRequiredService<IEntryRepository>()
            .TryClaimNextAsync(
                options.Value.InstanceName,
                now,
                now - settings.GenerationWait,
                now - settings.ClaimTimeout,
                now - settings.RetryFailedAfter,
                options.Value.MaxAttempts,
                cancellationToken);
        return (entryId, settings);
    }

    private async Task<CachedResponse?> GenerateAsync(long entryId, HoneyslopSettings settings, CancellationToken cancellationToken)
    {
        await using var asyncScope = serviceScopeFactory.CreateAsyncScope();
        var entryRepository = asyncScope.ServiceProvider.GetRequiredService<IEntryRepository>();

        var entry = await entryRepository.GetByIdOrDefaultAsync(entryId, cancellationToken);
        if (entry is not { Status: EntryStatus.Pending })
            return null;

        var instance = options.Value.InstanceName;
        var startedAt = timeProvider.GetTimestamp();
        using var heartbeat = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var beating = BeatAsync(entryId, settings.HeartbeatInterval, heartbeat.Token);
        var progress = new LatestProgress();
        var streaming = StreamOutputAsync(entryId, progress, heartbeat.Token);
        try
        {
            var completion = await llmDispatcher.CompleteAsync(
                promptBuilder.Build(entry, settings.AllowRefusals),
                settings.GenerationTimeLimit,
                progress,
                cancellationToken);
            if (completion.FinishReason == "length")
                throw new LlmException("Model output was cut at the token limit.");

            var generated = responseSanitizer.Sanitize(LlmResponseParser.Parse(completion.Content), entry.Host);
            var elapsed = timeProvider.GetElapsedTime(startedAt);
            var stored = await entryRepository.CompleteAsync(
                entryId,
                instance,
                new EntryResponse(
                    generated.Status,
                    generated.Headers,
                    generated.Body,
                    completion.Endpoint is null ? completion.Model : $"{completion.Model}@{completion.Endpoint}",
                    completion.PromptTokens,
                    completion.CompletionTokens,
                    (int)elapsed.TotalMilliseconds),
                timeProvider.GetUtcNow(),
                cancellationToken);
            if (!stored)
                LogLostOwnership(logger, entryId);

            TrapMetrics.Generations.WithLabels("ready").Inc();
            TrapMetrics.GenerationDuration.Observe(elapsed.TotalSeconds);
            TrapMetrics.CompletionTokens.Inc(completion.CompletionTokens ?? 0);
            LogGenerated(
                logger,
                entryId,
                entry.Method,
                entry.Host,
                entry.Target,
                generated.Status,
                (int)elapsed.TotalMilliseconds,
                completion.CompletionTokens);
            return CachedResponse.From(entryId, generated);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await entryRepository.RequeueAsync(entryId, instance, "Trap instance stopped during the generation.", timeProvider.GetUtcNow(), shutdown.Token);
            throw;
        }
        catch (LlmUnavailableException exception)
        {
            await entryRepository.RequeueAsync(entryId, instance, exception.Message, timeProvider.GetUtcNow(), cancellationToken);
            generationSignal.Notify();
            TrapMetrics.Generations.WithLabels("requeued").Inc();
            LogRequeued(logger, entryId, entry.Method, entry.Host, entry.Target, exception.Message);
            return null;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            await entryRepository.FailAsync(
                entryId,
                instance,
                exception.Message,
                timeProvider.GetUtcNow(),
                cancellationToken);

            TrapMetrics.Generations.WithLabels("failed").Inc();
            LogFailed(
                logger,
                entryId,
                entry.Method,
                entry.Host,
                entry.Target,
                entry.Attempts + 1,
                (int)timeProvider.GetElapsedTime(startedAt).TotalMilliseconds,
                exception.Message);
            return null;
        }
        finally
        {
            await heartbeat.CancelAsync();
            await Task.WhenAll(beating, streaming);
        }
    }

    private async Task StreamOutputAsync(long entryId, LatestProgress progress, CancellationToken cancellationToken)
    {
        try
        {
            LlmProgress? published = null;
            using var timer = new PeriodicTimer(OutputInterval, timeProvider);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (progress.Value is not { } current || ReferenceEquals(current, published))
                    continue;

                await using var asyncScope = serviceScopeFactory.CreateAsyncScope();
                await asyncScope
                    .ServiceProvider
                    .GetRequiredService<IEntryRepository>()
                    .NotifyOutputAsync(new GenerationOutput(entryId, current.Tokens, current.Content), cancellationToken);
                published = current;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            LogOutputFailed(logger, exception, entryId);
        }
    }

    private async Task BeatAsync(long entryId, TimeSpan interval, CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(interval, timeProvider);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await using var asyncScope = serviceScopeFactory.CreateAsyncScope();
                if (!await asyncScope
                        .ServiceProvider
                        .GetRequiredService<IEntryRepository>()
                        .HeartbeatAsync(entryId, options.Value.InstanceName, timeProvider.GetUtcNow(), cancellationToken))
                    return;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            LogHeartbeatFailed(logger, exception, entryId);
        }
    }

    [LoggerMessage(LogLevel.Warning, "Entry {entryId} was taken over by another instance; its result is served here but not stored")]
    static partial void LogLostOwnership(ILogger<GenerationWorker> logger, long entryId);

    [LoggerMessage(LogLevel.Warning, "Heartbeat of entry {entryId} failed")]
    static partial void LogHeartbeatFailed(ILogger<GenerationWorker> logger, Exception exception, long entryId);

    [LoggerMessage(LogLevel.Warning, "Streaming the output of entry {entryId} failed")]
    static partial void LogOutputFailed(ILogger<GenerationWorker> logger, Exception exception, long entryId);

    [LoggerMessage(LogLevel.Error, "Generation loop failed")]
    static partial void LogLoopFailed(ILogger<GenerationWorker> logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "Generated entry {entryId} {method} {host}{target} -> {status} in {elapsedMs} ms, {completionTokens} tokens")]
    static partial void LogGenerated(
        ILogger<GenerationWorker> logger,
        long entryId,
        string method,
        string host,
        string target,
        int status,
        int elapsedMs,
        int? completionTokens);

    [LoggerMessage(LogLevel.Warning, "Generation of entry {entryId} {method} {host}{target} went back to the queue: {reason}")]
    static partial void LogRequeued(
        ILogger<GenerationWorker> logger,
        long entryId,
        string method,
        string host,
        string target,
        string reason);

    [LoggerMessage(LogLevel.Warning, "Generation of entry {entryId} {method} {host}{target} failed on attempt {attempt} after {elapsedMs} ms: {error}")]
    static partial void LogFailed(
        ILogger<GenerationWorker> logger,
        long entryId,
        string method,
        string host,
        string target,
        int attempt,
        int elapsedMs,
        string error);
}
