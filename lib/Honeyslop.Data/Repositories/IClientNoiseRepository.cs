using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;

namespace Honeyslop.Data.Repositories;

public interface IClientNoiseRepository
{
    Task<ClientNoise[]> GetAllAsync(CancellationToken cancellationToken);
    Task<ClientNoise?> GetOrDefaultAsync(string clientIp, CancellationToken cancellationToken);
    Task SetAsync(string clientIp, NoiseTier? tier, string? changedBy, DateTimeOffset now, CancellationToken cancellationToken);
}