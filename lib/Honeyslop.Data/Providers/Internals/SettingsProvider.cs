using Honeyslop.Data.Contracts;
using Honeyslop.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Honeyslop.Data.Providers.Internals;

internal sealed partial class SettingsProvider(
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider timeProvider,
    ILogger<SettingsProvider> logger) : ISettingsProvider
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(10);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private DateTimeOffset _loadedAt = DateTimeOffset.MinValue;
    private HoneyslopSettings _settings = HoneyslopSettings.Default;

    public async ValueTask<HoneyslopSettings> GetAsync(CancellationToken cancellationToken)
    {
        if (timeProvider.GetUtcNow() - _loadedAt < RefreshInterval)
            return _settings;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (timeProvider.GetUtcNow() - _loadedAt < RefreshInterval)
                return _settings;

            try
            {
                await using var asyncScope = serviceScopeFactory.CreateAsyncScope();
                var settings = await asyncScope
                    .ServiceProvider
                    .GetRequiredService<ISettingsRepository>()
                    .GetAsync(null, cancellationToken);
                _settings = settings.Honeyslop;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                LogRefreshFailed(logger, exception);
            }

            _loadedAt = timeProvider.GetUtcNow();
            return _settings;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public void Reset()
    {
        _loadedAt = DateTimeOffset.MinValue;
    }

    [LoggerMessage(LogLevel.Warning, "Failed to load settings, keeping the previous ones")]
    static partial void LogRefreshFailed(ILogger<SettingsProvider> logger, Exception exception);
}