using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;

namespace Honeyslop.Data.Repositories;

public interface IRequestRepository
{
    Task AddRangeAsync(IReadOnlyCollection<RequestRecord> records, CancellationToken cancellationToken);
    Task UpdateRangeAsync(IReadOnlyCollection<RequestRecord> records, CancellationToken cancellationToken);
    Task<RequestRecord?> GetByIdOrDefaultAsync(long id, CancellationToken cancellationToken);

    Task<RequestRecord[]> GetPageAsync(
        string? clientIp,
        string? host,
        string? target,
        RequestOutcome? outcome,
        long? entryId,
        string? method,
        string? userAgent,
        DateTimeOffset? since,
        DateTimeOffset? until,
        RequestOrder order,
        SortDirection direction,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> CountAsync(
        string? clientIp,
        string? host,
        string? target,
        RequestOutcome? outcome,
        long? entryId,
        string? method,
        string? userAgent,
        DateTimeOffset? since,
        DateTimeOffset? until,
        long? afterId,
        CancellationToken cancellationToken);

    Task<RequestBucket[]> GetBreakdownAsync(
        string? clientIp,
        string? host,
        string? target,
        RequestOutcome? outcome,
        long? entryId,
        string? method,
        string? userAgent,
        DateTimeOffset? since,
        DateTimeOffset? until,
        RequestDimension dimension,
        int top,
        CancellationToken cancellationToken);

    Task<ClientActivity[]> GetClientsAsync(
        DateTimeOffset since,
        ClientOrder order,
        SortDirection direction,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> CountClientsAsync(DateTimeOffset since, CancellationToken cancellationToken);
    Task<ClientActivity?> GetClientOrDefaultAsync(string clientIp, CancellationToken cancellationToken);
    Task<long?> GetLogStartAsync(string clientIp, int latest, CancellationToken cancellationToken);
    Task<RequestRecord[]> GetLogAsync(string clientIp, long fromId, int limit, CancellationToken cancellationToken);
    Task<RequestRecord[]> GetTimingsAsync(IReadOnlyCollection<string> clientIps, long afterId, int limit, CancellationToken cancellationToken);
    Task<string[]> GetClientIpsAsync(CancellationToken cancellationToken);
    Task<int> DeleteReceivedBeforeAsync(DateTimeOffset threshold, CancellationToken cancellationToken);
}