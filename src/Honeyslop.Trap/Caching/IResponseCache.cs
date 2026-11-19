using System.Diagnostics.CodeAnalysis;
using Honeyslop.Data.Requests;
using Honeyslop.Trap.Responses;

namespace Honeyslop.Trap.Caching;

public interface IResponseCache
{
    long Version { get; }
    bool TryGet(EntryKey key, [NotNullWhen(true)] out CachedResponse? response);
    void Set(EntryKey key, CachedResponse response, long version);
    void Evict(long entryId);
    void Clear();
    void Enable();
    void Disable();
}