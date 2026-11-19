using Honeyslop.Llm.Contracts;

namespace Honeyslop.Admin.Core.Services;

public interface IGenerationOutputStore
{
    void Set(GenerationOutputDto output);
    GenerationOutputDto? GetOrDefault(long entryId);
}