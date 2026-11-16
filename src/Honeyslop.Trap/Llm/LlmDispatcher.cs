using Honeyslop.Llm;
using System.Collections.Concurrent;
using Honeyslop.Trap.Monitoring;
using Honeyslop.Trap.Options;
using Microsoft.Extensions.Options;

namespace Honeyslop.Trap.Llm;

internal sealed partial class LlmDispatcher(
    ILlmEndpointProvider endpointProvider,
    ILlmClient llmClient,
    IOptions<LlmOptions> options,
    TimeProvider timeProvider,
    ILogger<LlmDispatcher> logger) : ILlmDispatcher
{
    private readonly SemaphoreSlim _acquireLock = new(1, 1);
    private readonly ConcurrentDictionary<Uri, EndpointState> _states = new();

    public async Task<LlmCompletion> CompleteAsync(
        LlmPrompt prompt,
        TimeSpan timeLimit,
        IProgress<LlmProgress>? progress,
        CancellationToken cancellationToken)
    {
        var (endpoint, lease) = await AcquireAsync(cancellationToken);
        try
        {
            var completion = await llmClient.CompleteAsync(endpoint, prompt, timeLimit, progress, cancellationToken);
            return completion with
            {
                Endpoint = endpoint.Authority
            };
        }
        catch (LlmUnavailableException exception)
        {
            MarkUnhealthy(endpoint, exception.Message);
            throw;
        }
        finally
        {
            State(endpoint).Release(lease);
        }
    }

    private async Task<(Uri Endpoint, Lease Lease)> AcquireAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            await _acquireLock.WaitAsync(cancellationToken);
            try
            {
                var now = timeProvider.GetUtcNow();
                var discovered = await endpointProvider.GetEndpointsAsync(cancellationToken);
                TrapMetrics.LlmEndpoints.Set(discovered.Count);
                var endpoints = discovered
                    .Where(x => State(x).IsHealthy(now))
                    .ToArray();
                var probes = await Task.WhenAll(endpoints.Select(async x => (
                    Endpoint: x,
                    Idle: await llmClient.GetIdleSlotsAsync(x, cancellationToken))));

                Uri? best = null;
                var bestScore = 0;
                var bestInFlight = int.MaxValue;
                foreach (var (endpoint, idle) in probes.OrderBy(_ => Random.Shared.Next()))
                {
                    if (idle is null)
                    {
                        MarkUnhealthy(endpoint, "slot probe failed");
                        continue;
                    }

                    var state = State(endpoint);
                    var score = idle.Value - state.DispatchedSince(now - options.Value.DispatchGrace);
                    if (score <= 0 || (score < bestScore || score == bestScore && state.InFlight >= bestInFlight))
                        continue;

                    best = endpoint;
                    bestScore = score;
                    bestInFlight = state.InFlight;
                }

                if (best is not null)
                    return (best, State(best).Acquire(now));
            }
            finally
            {
                _acquireLock.Release();
            }

            TrapMetrics.LlmBusyWaits.Inc();
            await Task.Delay(options.Value.SlotPollInterval, timeProvider, cancellationToken);
        }
    }

    private void MarkUnhealthy(Uri endpoint, string reason)
    {
        var now = timeProvider.GetUtcNow();
        if (State(endpoint).MarkUnhealthy(now, now + options.Value.UnhealthyCooldown))
            LogUnhealthy(logger, endpoint.Authority, reason);
    }

    private EndpointState State(Uri endpoint)
    {
        return _states.GetOrAdd(endpoint, static x => new EndpointState(x.Authority));
    }

    [LoggerMessage(LogLevel.Warning, "Model server {endpoint} is skipped for a while: {reason}")]
    static partial void LogUnhealthy(ILogger<LlmDispatcher> logger, string endpoint, string reason);

    private sealed record Lease(DateTimeOffset StartedAt);

    private sealed class EndpointState(string authority)
    {
        private readonly List<Lease> _leases = [];
        private readonly Lock _lock = new();
        private DateTimeOffset _unhealthyUntil = DateTimeOffset.MinValue;

        public int InFlight
        {
            get
            {
                lock (_lock)
                    return _leases.Count;
            }
        }

        public bool IsHealthy(DateTimeOffset now)
        {
            lock (_lock)
                return _unhealthyUntil <= now;
        }

        public bool MarkUnhealthy(DateTimeOffset now, DateTimeOffset until)
        {
            lock (_lock)
            {
                var wasHealthy = _unhealthyUntil <= now;
                _unhealthyUntil = until;
                return wasHealthy;
            }
        }

        public int DispatchedSince(DateTimeOffset threshold)
        {
            lock (_lock)
                return _leases.Count(x => x.StartedAt > threshold);
        }

        public Lease Acquire(DateTimeOffset now)
        {
            var lease = new Lease(now);
            lock (_lock)
                _leases.Add(lease);
            TrapMetrics.LlmInFlight.WithLabels(authority).Inc();
            return lease;
        }

        public void Release(Lease lease)
        {
            lock (_lock)
                _leases.Remove(lease);
            TrapMetrics.LlmInFlight.WithLabels(authority).Dec();
        }
    }
}
