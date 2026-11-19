using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Honeyslop.Data.Requests;
using Honeyslop.Trap.Options;
using Honeyslop.Trap.Responses;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Honeyslop.Trap.Caching.Internals;

internal sealed class ResponseCache : IResponseCache, IDisposable
{
    private readonly MemoryCache _cache;
    private readonly TimeSpan _duration;
    private readonly Lock _lock = new();
    private readonly ConcurrentDictionary<long, string> _keys = new();
    private bool _enabled;
    private long _version;

    public ResponseCache(IOptions<TrapOptions> options)
    {
        _duration = options.Value.MemoryCacheDuration;
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = options.Value.MemoryCacheSizeLimit
        });
    }

    public long Version => Interlocked.Read(ref _version);

    public void Dispose()
    {
        _cache.Dispose();
    }

    public bool TryGet(EntryKey key, [NotNullWhen(true)] out CachedResponse? response)
    {
        return _cache.TryGetValue(key.HashHex, out response);
    }

    public void Set(EntryKey key, CachedResponse response, long version)
    {
        if (_duration <= TimeSpan.Zero)
            return;

        lock (_lock)
        {
            if (!_enabled || _version != version)
                return;
            if (_cache.TryGetValue(key.HashHex, out CachedResponse? previous) && previous!.EntryId != response.EntryId)
                _keys.TryRemove(new KeyValuePair<long, string>(previous.EntryId, key.HashHex));
            _keys[response.EntryId] = key.HashHex;
            _cache.Set(
                key.HashHex,
                response,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _duration,
                    Size = response.Body.Length + 1024,
                    PostEvictionCallbacks =
                    {
                        new PostEvictionCallbackRegistration
                        {
                            EvictionCallback = Forget
                        }
                    }
                });
        }
    }

    public void Evict(long entryId)
    {
        lock (_lock)
        {
            _version++;
            if (_keys.TryRemove(entryId, out var key) &&
                _cache.TryGetValue(key, out CachedResponse? cached) &&
                cached!.EntryId == entryId)
                _cache.Remove(key);
        }
    }

    public void Clear()
    {
        lock (_lock)
            Drop();
    }

    public void Enable()
    {
        lock (_lock)
        {
            Drop();
            _enabled = true;
        }
    }

    public void Disable()
    {
        lock (_lock)
        {
            Drop();
            _enabled = false;
        }
    }

    private void Drop()
    {
        _version++;
        _keys.Clear();
        _cache.Clear();
    }

    private void Forget(object key, object? value, EvictionReason reason, object? state)
    {
        if (value is CachedResponse response)
            _keys.TryRemove(new KeyValuePair<long, string>(response.EntryId, (string)key));
    }
}