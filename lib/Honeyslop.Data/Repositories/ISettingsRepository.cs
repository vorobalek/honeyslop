namespace Honeyslop.Data.Repositories;

public interface ISettingsRepository
{
    Task<HoneyslopSettings> GetAsync(CancellationToken cancellationToken);
    Task SaveAsync(HoneyslopSettings settings, DateTimeOffset now, CancellationToken cancellationToken);
}
