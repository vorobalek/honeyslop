using Honeyslop.Data.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace Honeyslop.Admin.Core.Access.Internals;

internal sealed class PublicView(
    IServiceScopeFactory serviceScopeFactory,
    IMemoryCache memoryCache) : IPublicView
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(5);

    public async ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken)
    {
        if (memoryCache.TryGetValue(nameof(PublicView), out bool enabled))
            return enabled;

        await using var asyncScope = serviceScopeFactory.CreateAsyncScope();
        var settings = await asyncScope
            .ServiceProvider
            .GetRequiredService<ISettingsRepository>()
            .GetAsync(null, cancellationToken);
        return memoryCache.Set(nameof(PublicView), settings.Honeyslop.PublicView, RefreshInterval);
    }

    public void Reset()
    {
        memoryCache.Remove(nameof(PublicView));
    }
}