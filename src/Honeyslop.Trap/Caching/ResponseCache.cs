using System.Diagnostics.CodeAnalysis;
using Honeyslop.Trap.Options;
using Honeyslop.Trap.Requests;
using Honeyslop.Trap.Responses;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Honeyslop.Trap.Caching;

internal sealed class ResponseCache : IResponseCache, IDisposable
{
    private readonly MemoryCache _cache;
    private readonly TimeSpan _duration;

    public ResponseCache(IOptions<TrapOptions> options)
    {
        _duration = options.Value.MemoryCacheDuration;
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = options.Value.MemoryCacheSizeLimit
        });
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    public bool TryGet(EntryKey key, [NotNullWhen(true)] out CachedResponse? response)
    {
        return _cache.TryGetValue(key.HashHex, out response);
    }

    public void Set(EntryKey key, CachedResponse response)
    {
        if (_duration <= TimeSpan.Zero)
            return;

        _cache.Set(
            key.HashHex,
            response,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _duration,
                Size = response.Body.Length + 1024
            });
    }
}
