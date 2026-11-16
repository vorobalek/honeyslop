using Honeyslop.Data.Entities;

namespace Honeyslop.Data.Repositories;

public interface IRequestRepository
{
    Task AddRangeAsync(IReadOnlyCollection<RequestRecord> records, CancellationToken cancellationToken);
    Task UpdateRangeAsync(IReadOnlyCollection<RequestRecord> records, CancellationToken cancellationToken);
    Task<RequestRecord?> GetByIdOrDefaultAsync(long id, CancellationToken cancellationToken);

    Task<RequestSummary[]> GetPageAsync(
        RequestFilter filter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> CountAsync(RequestFilter filter, CancellationToken cancellationToken);

    Task<ClientSummary[]> GetClientsAsync(
        DateTimeOffset since,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> CountClientsAsync(DateTimeOffset since, CancellationToken cancellationToken);
    Task<int> DeleteReceivedBeforeAsync(DateTimeOffset threshold, CancellationToken cancellationToken);
}
