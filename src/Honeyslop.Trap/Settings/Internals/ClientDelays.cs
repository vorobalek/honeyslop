using System.Collections.Frozen;
using Honeyslop.Data.Repositories;

namespace Honeyslop.Trap.Settings.Internals;

internal sealed partial class ClientDelays(
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider timeProvider,
    ILogger<ClientDelays> logger) : IClientDelays
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ChangedRefreshInterval = TimeSpan.FromSeconds(1);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private DateTimeOffset _loadedAt = DateTimeOffset.MinValue;
    private volatile bool _changed;
    private Snapshot _snapshot = new(FrozenDictionary<string, TimeSpan>.Empty, FrozenDictionary<string, TimeSpan>.Empty);

    public async ValueTask<PersonalDelay> GetAsync(string clientIp, CancellationToken cancellationToken)
    {
        var snapshot = Fresh ? _snapshot : await RefreshAsync(cancellationToken);
        return new PersonalDelay(
            snapshot.Manual.TryGetValue(clientIp, out var manual) ? manual : null,
            snapshot.Learned.TryGetValue(clientIp, out var learned) ? learned : null);
    }

    public void Reset()
    {
        _changed = true;
    }

    private bool Fresh => timeProvider.GetUtcNow() - _loadedAt < (_changed ? ChangedRefreshInterval : RefreshInterval);

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
                _snapshot = new Snapshot(
                    manual.ToFrozenDictionary(x => x.ClientIp, x => TimeSpan.FromMilliseconds(x.ResponseDelayMilliseconds), StringComparer.Ordinal),
                    learned.ToFrozenDictionary(x => x.ClientIp, x => TimeSpan.FromMilliseconds(x.DelayMilliseconds), StringComparer.Ordinal));
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

    [LoggerMessage(LogLevel.Warning, "Failed to load client delays, keeping the previous ones")]
    static partial void LogRefreshFailed(ILogger<ClientDelays> logger, Exception exception);

    private sealed record Snapshot(FrozenDictionary<string, TimeSpan> Manual, FrozenDictionary<string, TimeSpan> Learned);
}