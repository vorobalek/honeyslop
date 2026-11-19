using Honeyslop.Data.Entities;

namespace Honeyslop.Data.Repositories;

public interface IClientDelayRepository
{
    Task<ClientDelay[]> GetAllAsync(CancellationToken cancellationToken);
    Task<ClientDelay?> GetOrDefaultAsync(string clientIp, CancellationToken cancellationToken);
    Task<ClientDelay[]> GetAnyOfAsync(IReadOnlyCollection<string> clientIps, CancellationToken cancellationToken);
    Task<int> ClearAsync(CancellationToken cancellationToken);
    Task SetAsync(string clientIp, int? milliseconds, string? changedBy, DateTimeOffset now, CancellationToken cancellationToken);
}