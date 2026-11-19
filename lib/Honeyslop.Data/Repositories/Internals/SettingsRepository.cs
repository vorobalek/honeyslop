using Honeyslop.Data.Contracts;
using Honeyslop.Data.Converters;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Entities.Infrastructure;
using Honeyslop.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class SettingsRepository(ApplicationDbContext dbContext) : ISettingsRepository
{
    public async Task<Settings> GetAsync(string? userId, CancellationToken cancellationToken)
    {
        var values = await dbContext
            .Settings
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);
        if (userId is null)
            return new Settings { Honeyslop = SettingsConverter.ToSettings(values) };

        var userValues = await dbContext
            .UserSettings
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);
        return new Settings
        {
            Honeyslop = SettingsConverter.ToSettings(values),
            User = SettingsConverter.ToUserSettings(userId, userValues)
        };
    }

    public async Task SaveAsync(HoneyslopSettings settings, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.LockAsync(ResourceLock.Settings, cancellationToken);
        var existing = await dbContext
            .Settings
            .ToDictionaryAsync(x => x.Key, cancellationToken);
        Apply(
            dbContext.Settings,
            existing,
            SettingsConverter.ToValues(settings),
            (key, value) => new Setting { Key = key, Value = value, UpdatedAt = now },
            now);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SaveAsync(UserSettings settings, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.LockAsync(ResourceLock.Settings, cancellationToken);
        var existing = await dbContext
            .UserSettings
            .Where(x => x.UserId == settings.UserId)
            .ToDictionaryAsync(x => x.Key, cancellationToken);
        Apply(
            dbContext.UserSettings,
            existing,
            SettingsConverter.ToValues(settings),
            (key, value) => new UserSetting { UserId = settings.UserId, Key = key, Value = value, UpdatedAt = now },
            now);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static void Apply<TRow>(
        DbSet<TRow> rows,
        Dictionary<string, TRow> existing,
        Dictionary<string, string?> values,
        Func<string, string, TRow> create,
        DateTimeOffset now)
        where TRow : class, ISettingRow
    {
        foreach (var (key, value) in values)
        {
            var row = existing.GetValueOrDefault(key);
            if (string.IsNullOrWhiteSpace(value))
            {
                if (row is not null)
                    rows.Remove(row);
            }
            else if (row is null)
                rows.Add(create(key, value));
            else if (row.Value != value)
            {
                row.Value = value;
                row.UpdatedAt = now;
            }
        }
    }
}
