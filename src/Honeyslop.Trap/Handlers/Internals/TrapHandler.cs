using System.Net;
using System.Text;
using Honeyslop.Common;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Data.Requests;
using Honeyslop.Trap.Caching;
using Honeyslop.Trap.Delays;
using Honeyslop.Trap.Generation;
using Honeyslop.Trap.Hits;
using Honeyslop.Trap.Monitoring;
using Honeyslop.Trap.Options;
using Honeyslop.Trap.Requests;
using Honeyslop.Trap.Responses;
using Honeyslop.Trap.Settings;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace Honeyslop.Trap.Handlers.Internals;

internal sealed partial class TrapHandler(
    IRequestClassifier requestClassifier,
    IResponseCache responseCache,
    IEntryRepository entryRepository,
    IServerTimeRepository serverTime,
    IGenerationSignal generationSignal,
    IEntryWatcher entryWatcher,
    IHitCounter hitCounter,
    ISettingsProvider settingsProvider,
    IClientDelays clientDelays,
    IRequestLog requestLog,
    IGlobalCancellationTokenSource globalCancellationTokenSource,
    IOptions<TrapOptions> options,
    TimeProvider timeProvider,
    ILogger<TrapHandler> logger) : ITrapHandler
{
    private const int ClientClosedRequest = 499;
    private const int MaxHeaderValueLength = 512;
    private const int MaxLoggedHeaderValueLength = 1024;

    private static readonly string[] SnapshotHeaders =
    [
        "User-Agent",
        "Accept",
        "Accept-Language",
        "Content-Type",
        "Content-Length",
        "Referer",
        "Origin",
        "X-Requested-With"
    ];

    public async Task HandleAsync(HttpContext httpContext)
    {
        var startedAt = timeProvider.GetTimestamp();
        var receivedAt = await serverTime.GetNowAsync(globalCancellationTokenSource.Token);
        var request = httpContext.Request;
        var host = (request.Host.HasValue ? request.Host.Host : string.Empty)
            .ToLowerInvariant()
            .Truncate(Entry.HostMaxLength);
        var target = httpContext.Features.Get<IHttpRequestFeature>()?.RawTarget is { Length: > 0 } rawTarget
            ? rawTarget
            : request.Path.ToUriComponent() + request.QueryString.ToUriComponent();
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        string? body = null;
        var outcome = RequestOutcome.Error;
        var status = StatusCodes.Status500InternalServerError;
        long? entryId = null;
        RequestRecord? record = null;

        RequestRecord Record()
        {
            return record ??= new RequestRecord
            {
                ReceivedAt = receivedAt,
                ClientIp = clientIp.Truncate(Entry.ClientIpMaxLength),
                Method = request.Method.Truncate(Entry.MethodMaxLength),
                Host = host,
                Target = target.Truncate(Entry.TargetMaxLength),
                UserAgent = UserAgentOf(request),
                Headers = RenderAllHeaders(request.Headers),
                Body = body,
                Instance = options.Value.InstanceName.Truncate(Entry.ClaimedByMaxLength)
            };
        }

        void LogWaiting(long waitingFor)
        {
            var waiting = Record();
            waiting.Outcome = RequestOutcome.Waiting;
            waiting.EntryId = waitingFor;
            requestLog.Add(waiting);
        }

        try
        {
            body = await ReadBodyAsync(httpContext);
            var classification = requestClassifier.Classify(request.Method, target);
            (outcome, var response) = classification.Kind == ClassificationKind.Generate
                ? await HandleGeneratedAsync(httpContext, classification.Method, host, target, body, receivedAt, LogWaiting)
                : (RequestOutcome.Static, StaticResponses.For(classification.Kind));
            status = response.Status;
            entryId = response.EntryId > 0 ? response.EntryId : null;
            if (outcome is RequestOutcome.Hit or RequestOutcome.Generated)
                await RespondLateAsync(httpContext, response, startedAt, httpContext.RequestAborted);
            else
                await ResponseWriter.WriteAsync(httpContext, response, httpContext.RequestAborted);
        }
        catch (Exception exception) when (exception is OperationCanceledException or IOException &&
                                          httpContext.RequestAborted.IsCancellationRequested)
        {
            outcome = RequestOutcome.Abandoned;
            status = ClientClosedRequest;
        }
        finally
        {
            var elapsed = timeProvider.GetElapsedTime(startedAt);
            var elapsedMs = (int)elapsed.TotalMilliseconds;
            var outcomeLabel = outcome.ToString().ToLowerInvariant();
            TrapMetrics.Requests.WithLabels(outcomeLabel).Inc();
            TrapMetrics.RequestDuration.WithLabels(outcomeLabel).Observe(elapsed.TotalSeconds);
            LogRequest(logger, outcome, request.Method, host, target, status, elapsedMs, clientIp);
            var loggedWhileWaiting = record is not null;
            var final = Record();
            final.Outcome = outcome;
            final.ResponseStatus = status;
            final.EntryId = entryId ?? final.EntryId;
            final.DurationMs = elapsedMs;
            if (loggedWhileWaiting)
                requestLog.Update(final);
            else
                requestLog.Add(final);
        }
    }

    private async Task<(RequestOutcome Outcome, CachedResponse Response)> HandleGeneratedAsync(
        HttpContext httpContext,
        string method,
        string host,
        string target,
        string? body,
        DateTimeOffset receivedAt,
        Action<long> onWaiting)
    {
        var cancellationToken = httpContext.RequestAborted;
        var key = EntryKey.Create(method, host, target);
        var cacheVersion = responseCache.Version;
        if (responseCache.TryGet(key, out var cached))
        {
            hitCounter.Add(cached.EntryId, receivedAt);
            return (RequestOutcome.Hit, cached);
        }

        var entry = await entryRepository.GetByKeyHashOrDefaultAsync(key.Hash, cancellationToken);
        if (entry is { Status: EntryStatus.Ready })
            return (RequestOutcome.Hit, ServeReady(key, entry, receivedAt, cacheVersion));

        var settings = await settingsProvider.GetAsync(cancellationToken);
        if (!settings.GenerationEnabled)
            return (RequestOutcome.Fallback, StaticResponses.NotFound);

        if (entry is not null)
            await entryRepository.TouchRequestedAsync(entry.Id, receivedAt, globalCancellationTokenSource.Token);
        entry ??= await EnqueueAsync(httpContext, key, body, receivedAt);
        if (entry is { Status: EntryStatus.Ready })
            return (RequestOutcome.Hit, ServeReady(key, entry, receivedAt, cacheVersion));
        if (entry is null)
            return (RequestOutcome.Fallback, StaticResponses.NotFound);
        if (entry.Status == EntryStatus.Failed && entry.Attempts >= options.Value.MaxAttempts)
            return (RequestOutcome.Fallback, StaticResponses.NotFound);

        onWaiting(entry.Id);
        var generated = await entryWatcher.WaitAsync(entry.Id, settings.GenerationWait, cancellationToken);
        if (generated is null)
            return (RequestOutcome.Fallback, StaticResponses.NotFound);

        generated = generated with
        {
            Delay = CachedResponse.DelayOf(entry)
        };
        responseCache.Set(key, generated, cacheVersion);
        hitCounter.Add(generated.EntryId, await serverTime.GetNowAsync(globalCancellationTokenSource.Token));
        return (RequestOutcome.Generated, generated);
    }

    private async Task RespondLateAsync(HttpContext httpContext, CachedResponse response, long startedAt, CancellationToken cancellationToken)
    {
        var settings = await settingsProvider.GetAsync(cancellationToken);
        var client = httpContext.Connection.RemoteIpAddress is { } address
            ? await clientDelays.GetAsync(ClientAddress.Key(address), cancellationToken)
            : new PersonalDelay(null, null);
        var learning = settings.AutoDelayEnabled;
        var (delay, source) = client.Manual is { } manual ? (manual, "manual")
            : learning && client.Learned is { } learned ? (learned, "learned")
            : learning ? (TimeSpan.FromMilliseconds(DelayLearner.Middle(settings.AutoDelayMinMilliseconds, settings.AutoDelayMaxMilliseconds)), "start")
            : response.Delay is { } entry ? (entry, "entry")
            : (settings.ResponseDelayMilliseconds is { } milliseconds ? TimeSpan.FromMilliseconds(milliseconds) : options.Value.ResponseDelay, "default");
        TrapMetrics.ResponseDelays.WithLabels(source).Inc();
        var remaining = delay - timeProvider.GetElapsedTime(startedAt);
        if (learning)
        {
            await ResponseWriter.TrickleAsync(httpContext, response, remaining, timeProvider, cancellationToken);
            return;
        }

        if (remaining > TimeSpan.Zero)
            await Task.Delay(remaining, timeProvider, cancellationToken);
        await ResponseWriter.WriteAsync(httpContext, response, cancellationToken);
    }

    private CachedResponse ServeReady(EntryKey key, Entry entry, DateTimeOffset servedAt, long cacheVersion)
    {
        var ready = CachedResponse.From(entry);
        responseCache.Set(key, ready, cacheVersion);
        hitCounter.Add(ready.EntryId, servedAt);
        return ready;
    }

    private async Task<Entry?> EnqueueAsync(HttpContext httpContext, EntryKey key, string? body, DateTimeOffset now)
    {
        var cancellationToken = globalCancellationTokenSource.Token;
        var queued = new Entry
        {
            KeyHash = key.Hash,
            Method = key.Method,
            Host = key.Host,
            Target = key.Target,
            Status = EntryStatus.Pending,
            ClientIp = httpContext.Connection.RemoteIpAddress?.ToString().Truncate(Entry.ClientIpMaxLength),
            UserAgent = UserAgentOf(httpContext.Request),
            RequestHeaders = RenderSnapshotHeaders(httpContext.Request.Headers),
            RequestBody = body,
            CreatedAt = now,
            UpdatedAt = now,
            RequestedAt = now
        };
        if (!await entryRepository.TryAddAsync(queued, cancellationToken))
            return await entryRepository.GetByKeyHashOrDefaultAsync(key.Hash, cancellationToken);

        generationSignal.Notify();
        return queued;
    }

    private static string? UserAgentOf(HttpRequest request)
    {
        return request.Headers.UserAgent is { Count: > 0 } userAgent
            ? userAgent.ToString().Truncate(Entry.UserAgentMaxLength)
            : null;
    }

    private static string? RenderSnapshotHeaders(IHeaderDictionary headers)
    {
        var builder = new StringBuilder();
        foreach (var name in SnapshotHeaders)
            if (headers.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
                builder
                    .Append(name)
                    .Append(": ")
                    .Append(value.ToString().ReplaceLineEndings(" ").Truncate(MaxHeaderValueLength))
                    .Append('\n');
        return builder.Length > 0 ? builder.ToString() : null;
    }

    private static string? RenderAllHeaders(IHeaderDictionary headers)
    {
        var builder = new StringBuilder();
        foreach (var (name, values) in headers)
        {
            var value = name.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
                ? string.Join("; ", values
                    .SelectMany(x => (x ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Select(x => x.Split('=', 2)[0] + "=…"))
                : values.ToString();
            builder
                .Append(name)
                .Append(": ")
                .Append(value.ReplaceLineEndings(" ").Truncate(MaxLoggedHeaderValueLength))
                .Append('\n');
            if (builder.Length >= RequestRecord.HeadersMaxLength)
                break;
        }

        return builder.Length > 0 ? builder.ToString().Truncate(RequestRecord.HeadersMaxLength) : null;
    }

    private async Task<string?> ReadBodyAsync(HttpContext httpContext)
    {
        var request = httpContext.Request;
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || options.Value.MaxRequestBodyBytes <= 0)
            return null;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(httpContext.RequestAborted);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        var buffer = new byte[options.Value.MaxRequestBodyBytes];
        try
        {
            var read = await request.Body.ReadAtLeastAsync(buffer, buffer.Length, false, timeout.Token);
            return read > 0 ? Encoding.UTF8.GetString(buffer, 0, read).Replace("\0", string.Empty) : null;
        }
        catch (Exception exception) when (exception is OperationCanceledException or IOException or BadHttpRequestException)
        {
            return null;
        }
    }

    [LoggerMessage(LogLevel.Information, "{outcome} {method} {host}{target} -> {status} in {elapsedMs} ms from {clientIp}")]
    static partial void LogRequest(
        ILogger<TrapHandler> logger,
        RequestOutcome outcome,
        string method,
        string host,
        string target,
        int status,
        int elapsedMs,
        string clientIp);
}