using System.Collections.Frozen;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Trap.Monitoring;

namespace Honeyslop.Trap.Settings.Internals;

internal sealed partial class ClientSettings(
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider timeProvider,
    ILogger<ClientSettings> logger) : IClientSettings
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ChangedRefreshInterval = TimeSpan.FromSeconds(1);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private volatile bool _changed;
    private DateTimeOffset _loadedAt = DateTimeOffset.MinValue;
    private Snapshot _snapshot = new(FrozenDictionary<string, TimeSpan>.Empty, FrozenDictionary<string, TimeSpan>.Empty, FrozenDictionary<string, NoiseTier>.Empty);

    private bool Fresh => timeProvider.GetUtcNow() - _loadedAt < (_changed ? ChangedRefreshInterval : RefreshInterval);

    public async ValueTask<PersonalSettings> GetAsync(string clientIp, CancellationToken cancellationToken)
    {
        var snapshot = Fresh ? _snapshot : await RefreshAsync(cancellationToken);
        return new PersonalSettings(
            snapshot.Manual.TryGetValue(clientIp, out var manual) ? manual : null,
            snapshot.Learned.TryGetValue(clientIp, out var learned) ? learned : null,
            snapshot.Noise.TryGetValue(clientIp, out var noise) ? noise : null);
    }

    public void Reset()
    {
        _changed = true;
    }

    private async Task<Snapshot> RefreshAsync(CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (Fresh)
                return _snapshot;

            _changed = false;
            try
            {
                await using var asyncScope = serviceScopeFactory.CreateAsyncScope();
                var manual = await asyncScope.ServiceProvider.GetRequiredService<IClientDelayRepository>().GetAllAsync(cancellationToken);
                var learned = await asyncScope.ServiceProvider.GetRequiredService<IClientProbeRepository>().GetAllAsync(cancellationToken);
                var noise = await asyncScope.ServiceProvider.GetRequiredService<IClientNoiseRepository>().GetAllAsync(cancellationToken);
                _snapshot = new Snapshot(
                    manual.ToFrozenDictionary(x => x.ClientIp, x => TimeSpan.FromMilliseconds(x.ResponseDelayMilliseconds), StringComparer.Ordinal),
                    learned.ToFrozenDictionary(x => x.ClientIp, x => TimeSpan.FromMilliseconds(x.DelayMilliseconds), StringComparer.Ordinal),
                    noise.ToFrozenDictionary(x => x.ClientIp, x => x.Tier, StringComparer.Ordinal));
                TrapMetrics.PersonalDelays.WithLabels("manual").Set(manual.Length);
                TrapMetrics.PersonalDelays.WithLabels("learned").Set(learned.Length);
                TrapMetrics.NoisyClients.Set(noise.Length);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                LogRefreshFailed(logger, exception);
            }

            _loadedAt = timeProvider.GetUtcNow();
            return _snapshot;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    [LoggerMessage(LogLevel.Warning, "Failed to load client settings, keeping the previous ones")]
    static partial void LogRefreshFailed(ILogger<ClientSettings> logger, Exception exception);

    private sealed record Snapshot(FrozenDictionary<string, TimeSpan> Manual,
        FrozenDictionary<string, TimeSpan> Learned,
        FrozenDictionary<string, NoiseTier> Noise);
}