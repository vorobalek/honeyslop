using Honeyslop.Data.Entities;

namespace Honeyslop.Data.Repositories;

public interface IClientProbeRepository
{
    Task<ClientProbe?> GetOrDefaultAsync(string clientIp, CancellationToken cancellationToken);
    Task<ClientProbe[]> GetAllAsync(CancellationToken cancellationToken);
    Task<ClientProbe[]> GetAnyOfAsync(IReadOnlyCollection<string> clientIps, CancellationToken cancellationToken);
    Task<bool> TryAddAsync(ClientProbe probe, CancellationToken cancellationToken);
    Task<bool> TryReplaceAsync(ClientProbe probe, int expectedVersion, CancellationToken cancellationToken);
    Task<int> ClearAsync(CancellationToken cancellationToken);
}