using System.Collections.Frozen;
using Honeyslop.Data.Repositories;

namespace Honeyslop.Trap.Settings.Internals;

internal sealed partial class ClientDelays(
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider timeProvider,
    ILogger<ClientDelays> logger) : IClientDelays
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(10);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private DateTimeOffset _loadedAt = DateTimeOffset.MinValue;
    private FrozenDictionary<string, TimeSpan> _delays = FrozenDictionary<string, TimeSpan>.Empty;

    public async ValueTask<TimeSpan?> GetAsync(string clientIp, CancellationToken cancellationToken)
    {
        var delays = timeProvider.GetUtcNow() - _loadedAt < RefreshInterval ? _delays : await RefreshAsync(cancellationToken);
        return delays.TryGetValue(clientIp, out var delay) ? delay : null;
    }

    public void Reset()
    {
        _loadedAt = DateTimeOffset.MinValue;
    }

    private async Task<FrozenDictionary<string, TimeSpan>> RefreshAsync(CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (timeProvider.GetUtcNow() - _loadedAt < RefreshInterval)
                return _delays;

            try
            {
                await using var asyncScope = serviceScopeFactory.CreateAsyncScope();
                var delays = await asyncScope
                    .ServiceProvider
                    .GetRequiredService<IClientDelayRepository>()
                    .GetAllAsync(cancellationToken);
                _delays = delays.ToFrozenDictionary(x => x.ClientIp, x => TimeSpan.FromMilliseconds(x.ResponseDelayMilliseconds), StringComparer.Ordinal);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                LogRefreshFailed(logger, exception);
            }

            _loadedAt = timeProvider.GetUtcNow();
            return _delays;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    [LoggerMessage(LogLevel.Warning, "Failed to load client delays, keeping the previous ones")]
    static partial void LogRefreshFailed(ILogger<ClientDelays> logger, Exception exception);
}