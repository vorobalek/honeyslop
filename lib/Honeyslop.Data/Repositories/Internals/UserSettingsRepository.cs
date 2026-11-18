using Honeyslop.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class UserSettingsRepository(ApplicationDbContext dbContext) : IUserSettingsRepository
{
    private const string SenderName = "sender_name";

    public async Task<UserSettings> GetAsync(string userId, CancellationToken cancellationToken)
    {
        var values = await dbContext
            .UserSettings
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);
        return new UserSettings(values.GetValueOrDefault(SenderName));
    }

    public async Task SaveAsync(string userId, UserSettings settings, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await dbContext
            .UserSettings
            .Where(x => x.UserId == userId)
            .ToDictionaryAsync(x => x.Key, cancellationToken);
        Set(SenderName, settings.SenderName);
        await dbContext.SaveChangesAsync(cancellationToken);

        void Set(string key, string? value)
        {
            var found = existing.TryGetValue(key, out var setting);
            if (string.IsNullOrWhiteSpace(value))
            {
                if (found)
                    dbContext.UserSettings.Remove(setting!);
                return;
            }

            if (found)
            {
                if (setting!.Value == value)
                    return;
                setting.Value = value;
                setting.UpdatedAt = now;
                return;
            }

            dbContext.UserSettings.Add(new UserSetting
            {
                UserId = userId,
                Key = key,
                Value = value,
                UpdatedAt = now
            });
        }
    }
}
