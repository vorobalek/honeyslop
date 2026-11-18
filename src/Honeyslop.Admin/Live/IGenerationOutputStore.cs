using Honeyslop.Data.Repositories;

namespace Honeyslop.Admin.Live;

public interface IGenerationOutputStore
{
    void Set(GenerationOutput output);
    GenerationOutput? GetOrDefault(long entryId);
}
