namespace Honeyslop.Data.Repositories;

public interface IUserSettingsRepository
{
    Task<UserSettings> GetAsync(string userId, CancellationToken cancellationToken);
    Task SaveAsync(string userId, UserSettings settings, DateTimeOffset now, CancellationToken cancellationToken);
}
