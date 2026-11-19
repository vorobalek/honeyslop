using Honeyslop.Trap.Options;
using Microsoft.Extensions.Options;

namespace Honeyslop.Trap.Tests;

internal static class TestOptions
{
    public static IOptions<TrapOptions> Trap(Action<TrapOptions>? configure = null)
    {
        var options = new TrapOptions
        {
            InstanceName = "test",
            Workers = 1,
            MaxWaiters = 10,
            MaxAttempts = 3,
            MaxRequestBodyBytes = 2048,
            MemoryCacheDuration = TimeSpan.FromSeconds(60),
            MemoryCacheSizeLimit = 1024 * 1024,
            HitFlushInterval = TimeSpan.FromSeconds(15)
        };
        configure?.Invoke(options);
        return new OptionsWrapper<TrapOptions>(options);
    }
}