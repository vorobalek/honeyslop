using Honeyslop.Llm.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace Honeyslop.Admin.Core.Services.Internals;

internal sealed class GenerationOutputStore(IMemoryCache memoryCache) : IGenerationOutputStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(1);

    public void Set(GenerationOutputDto output)
    {
        memoryCache.Set(Key(output.EntryId), output, Lifetime);
    }

    public GenerationOutputDto? GetOrDefault(long entryId)
    {
        return memoryCache.Get<GenerationOutputDto>(Key(entryId));
    }

    private static (string, long) Key(long entryId)
    {
        return (nameof(GenerationOutputDto), entryId);
    }
}
