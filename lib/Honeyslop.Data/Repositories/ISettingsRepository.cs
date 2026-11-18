using Honeyslop.Data.Contracts;

namespace Honeyslop.Data.Repositories;

public interface ISettingsRepository
{
    Task<Settings> GetAsync(string? userId, CancellationToken cancellationToken);
    Task SaveAsync(HoneyslopSettings settings, DateTimeOffset now, CancellationToken cancellationToken);
    Task SaveAsync(UserSettings settings, DateTimeOffset now, CancellationToken cancellationToken);
}
