using System.Globalization;
using Honeyslop.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class SettingsRepository(ApplicationDbContext dbContext) : ISettingsRepository
{
    private const string CacheTtlSeconds = "cache_ttl_seconds";
    private const string KeepUnusedSeconds = "keep_unused_seconds";
    private const string GenerationEnabled = "generation_enabled";
    private const string RequestRetentionSeconds = "request_retention_seconds";
    private const string GenerationWaitSeconds = "generation_wait_seconds";
    private const string AllowRefusals = "allow_refusals";
    private const string ClaimTimeoutSeconds = "claim_timeout_seconds";
    private const string RetryFailedSeconds = "retry_failed_seconds";
    private const string GenerationTimeLimitSeconds = "generation_time_limit_seconds";
    private const string PublicView = "public_view";

    public async Task<HoneyslopSettings> GetAsync(CancellationToken cancellationToken)
    {
        var values = await dbContext
            .Settings
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);
        var defaults = HoneyslopSettings.Default;
        return new HoneyslopSettings(
            ReadSeconds(values, CacheTtlSeconds, defaults.CacheTtl),
            ReadSeconds(values, KeepUnusedSeconds, defaults.KeepUnused),
            ReadFlag(values, GenerationEnabled, defaults.GenerationEnabled),
            ReadSeconds(values, RequestRetentionSeconds, defaults.RequestRetention),
            ReadSeconds(values, GenerationWaitSeconds, defaults.GenerationWait),
            ReadFlag(values, AllowRefusals, defaults.AllowRefusals),
            ReadSeconds(values, ClaimTimeoutSeconds, defaults.ClaimTimeout),
            ReadSeconds(values, RetryFailedSeconds, defaults.RetryFailedAfter),
            ReadSeconds(values, GenerationTimeLimitSeconds, defaults.GenerationTimeLimit),
            ReadFlag(values, PublicView, defaults.PublicView));
    }

    public async Task SaveAsync(HoneyslopSettings settings, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await dbContext
            .Settings
            .ToDictionaryAsync(x => x.Key, cancellationToken);
        Set(CacheTtlSeconds, ((long)settings.CacheTtl.TotalSeconds).ToString(CultureInfo.InvariantCulture));
        Set(KeepUnusedSeconds, ((long)settings.KeepUnused.TotalSeconds).ToString(CultureInfo.InvariantCulture));
        Set(GenerationEnabled, settings.GenerationEnabled.ToString(CultureInfo.InvariantCulture));
        Set(RequestRetentionSeconds, ((long)settings.RequestRetention.TotalSeconds).ToString(CultureInfo.InvariantCulture));
        Set(GenerationWaitSeconds, ((long)settings.GenerationWait.TotalSeconds).ToString(CultureInfo.InvariantCulture));
        Set(AllowRefusals, settings.AllowRefusals.ToString(CultureInfo.InvariantCulture));
        Set(ClaimTimeoutSeconds, ((long)settings.ClaimTimeout.TotalSeconds).ToString(CultureInfo.InvariantCulture));
        Set(RetryFailedSeconds, ((long)settings.RetryFailedAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture));
        Set(GenerationTimeLimitSeconds, ((long)settings.GenerationTimeLimit.TotalSeconds).ToString(CultureInfo.InvariantCulture));
        Set(PublicView, settings.PublicView.ToString(CultureInfo.InvariantCulture));
        await dbContext.SaveChangesAsync(cancellationToken);

        void Set(string key, string value)
        {
            if (existing.TryGetValue(key, out var setting))
            {
                if (setting.Value == value)
                    return;
                setting.Value = value;
                setting.UpdatedAt = now;
                return;
            }

            dbContext.Settings.Add(new Setting
            {
                Key = key,
                Value = value,
                UpdatedAt = now
            });
        }
    }

    private static bool ReadFlag(Dictionary<string, string> values, string key, bool fallback)
    {
        return values.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }

    private static TimeSpan ReadSeconds(Dictionary<string, string> values, string key, TimeSpan fallback)
    {
        return values.TryGetValue(key, out var value) &&
               long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) &&
               seconds >= 0
            ? TimeSpan.FromSeconds(seconds)
            : fallback;
    }
}
