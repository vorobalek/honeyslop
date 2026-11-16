using System.Diagnostics.CodeAnalysis;
using Honeyslop.Trap.Requests;
using Honeyslop.Trap.Responses;

namespace Honeyslop.Trap.Caching;

public interface IResponseCache
{
    bool TryGet(EntryKey key, [NotNullWhen(true)] out CachedResponse? response);
    void Set(EntryKey key, CachedResponse response);
}
