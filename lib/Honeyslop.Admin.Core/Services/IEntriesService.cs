using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Llm.Contracts;

namespace Honeyslop.Admin.Core.Services;

public interface IEntriesService
{
    const int PageSize = 50;

    Task<EntriesPageDto> GetPageAsync(EntriesQuery query, CancellationToken cancellationToken);
    Task<EntryDetailsDto?> GetAsync(long id, CancellationToken cancellationToken);
    Task<GenerationOutputDto?> GetOutputAsync(long id, CancellationToken cancellationToken);
    Task<string?> GetPreviewAsync(long id, CancellationToken cancellationToken);
    Task<int> ExpireAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken);
    Task<int> ExpireMatchingAsync(EntryFilter filter, CancellationToken cancellationToken);
}
