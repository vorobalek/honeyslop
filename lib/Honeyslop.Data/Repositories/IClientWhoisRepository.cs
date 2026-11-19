using Honeyslop.Data.Entities;

namespace Honeyslop.Data.Repositories;

public interface IClientWhoisRepository
{
    Task<ClientWhois?> GetOrDefaultAsync(string clientIp, CancellationToken cancellationToken);
    Task SaveAsync(string clientIp, string info, DateTimeOffset now, CancellationToken cancellationToken);
}