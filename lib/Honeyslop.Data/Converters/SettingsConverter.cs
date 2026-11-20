using System.Globalization;
using Honeyslop.Data.Contracts;

namespace Honeyslop.Data.Converters;

internal static class SettingsConverter
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
    private const string UserAgent = "user_agent";
    private const string ResponseDelayMilliseconds = "response_delay_milliseconds";
    private const string AutoDelayEnabled = "auto_delay_enabled";
    private const string AutoDelayMinMilliseconds = "auto_delay_min_milliseconds";
    private const string AutoDelayMaxMilliseconds = "auto_delay_max_milliseconds";
    private const string NoiseEnabled = "noise_enabled";
    private const string SenderName = "sender_name";

    public static HoneyslopSettings ToSettings(IReadOnlyDictionary<string, string> values)
    {
        var defaults = HoneyslopSettings.Default;
        return new HoneyslopSettings
        {
            CacheTtl = ReadSeconds(values, CacheTtlSeconds, defaults.CacheTtl),
            KeepUnused = ReadSeconds(values, KeepUnusedSeconds, defaults.KeepUnused),
            GenerationEnabled = ReadFlag(values, GenerationEnabled, defaults.GenerationEnabled),
            RequestRetention = ReadSeconds(values, RequestRetentionSeconds, defaults.RequestRetention),
            GenerationWait = ReadSeconds(values, GenerationWaitSeconds, defaults.GenerationWait),
            AllowRefusals = ReadFlag(values, AllowRefusals, defaults.AllowRefusals),
            ClaimTimeout = ReadSeconds(values, ClaimTimeoutSeconds, defaults.ClaimTimeout),
            RetryFailedAfter = ReadSeconds(values, RetryFailedSeconds, defaults.RetryFailedAfter),
            GenerationTimeLimit = ReadSeconds(values, GenerationTimeLimitSeconds, defaults.GenerationTimeLimit),
            PublicView = ReadFlag(values, PublicView, defaults.PublicView),
            UserAgent = values.GetValueOrDefault(UserAgent),
            ResponseDelayMilliseconds = ReadOptionalInteger(values, ResponseDelayMilliseconds),
            AutoDelayEnabled = ReadFlag(values, AutoDelayEnabled, defaults.AutoDelayEnabled),
            AutoDelayMinMilliseconds = ReadOptionalInteger(values, AutoDelayMinMilliseconds) ?? defaults.AutoDelayMinMilliseconds,
            AutoDelayMaxMilliseconds = ReadOptionalInteger(values, AutoDelayMaxMilliseconds) ?? defaults.AutoDelayMaxMilliseconds,
            NoiseEnabled = ReadFlag(values, NoiseEnabled, defaults.NoiseEnabled)
        };
    }

    public static Dictionary<string, string?> ToValues(HoneyslopSettings settings)
    {
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [CacheTtlSeconds] = Seconds(settings.CacheTtl),
            [KeepUnusedSeconds] = Seconds(settings.KeepUnused),
            [GenerationEnabled] = settings.GenerationEnabled.ToString(CultureInfo.InvariantCulture),
            [RequestRetentionSeconds] = Seconds(settings.RequestRetention),
            [GenerationWaitSeconds] = Seconds(settings.GenerationWait),
            [AllowRefusals] = settings.AllowRefusals.ToString(CultureInfo.InvariantCulture),
            [ClaimTimeoutSeconds] = Seconds(settings.ClaimTimeout),
            [RetryFailedSeconds] = Seconds(settings.RetryFailedAfter),
            [GenerationTimeLimitSeconds] = Seconds(settings.GenerationTimeLimit),
            [PublicView] = settings.PublicView.ToString(CultureInfo.InvariantCulture),
            [UserAgent] = settings.UserAgent,
            [ResponseDelayMilliseconds] = settings.ResponseDelayMilliseconds?.ToString(CultureInfo.InvariantCulture),
            [AutoDelayEnabled] = settings.AutoDelayEnabled.ToString(CultureInfo.InvariantCulture),
            [AutoDelayMinMilliseconds] = settings.AutoDelayMinMilliseconds.ToString(CultureInfo.InvariantCulture),
            [AutoDelayMaxMilliseconds] = settings.AutoDelayMaxMilliseconds.ToString(CultureInfo.InvariantCulture),
            [NoiseEnabled] = settings.NoiseEnabled.ToString(CultureInfo.InvariantCulture)
        };
    }

    public static UserSettings ToUserSettings(string userId, IReadOnlyDictionary<string, string> values)
    {
        return new UserSettings
        {
            UserId = userId,
            SenderName = values.GetValueOrDefault(SenderName)
        };
    }

    public static Dictionary<string, string?> ToValues(UserSettings settings)
    {
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [SenderName] = settings.SenderName
        };
    }

    private static string Seconds(TimeSpan duration)
    {
        return ((long)duration.TotalSeconds).ToString(CultureInfo.InvariantCulture);
    }

    private static bool ReadFlag(IReadOnlyDictionary<string, string> values, string key, bool fallback)
    {
        return values.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }

    private static TimeSpan ReadSeconds(IReadOnlyDictionary<string, string> values, string key, TimeSpan fallback)
    {
        return values.TryGetValue(key, out var value) &&
               long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) &&
               seconds >= 0
            ? TimeSpan.FromSeconds(seconds)
            : fallback;
    }

    private static int? ReadOptionalInteger(IReadOnlyDictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var value) &&
               int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) &&
               number >= 0
            ? number
            : null;
    }
}