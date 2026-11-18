using Honeyslop.Data.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace Honeyslop.Admin.Live;

internal sealed class GenerationOutputStore(IMemoryCache memoryCache) : IGenerationOutputStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(1);

    public void Set(GenerationOutput output)
    {
        memoryCache.Set(Key(output.EntryId), output, Lifetime);
    }

    public GenerationOutput? GetOrDefault(long entryId)
    {
        return memoryCache.Get<GenerationOutput>(Key(entryId));
    }

    private static (string, long) Key(long entryId)
    {
        return (nameof(GenerationOutput), entryId);
    }
}
