namespace Honeyslop.Admin.Views.Contracts;

public sealed record SettingsFormDto(
    long CacheTtlSeconds,
    long KeepUnusedSeconds,
    bool GenerationEnabled,
    long RequestRetentionSeconds,
    long GenerationWaitSeconds,
    bool AllowRefusals,
    long ClaimTimeoutSeconds,
    long RetryFailedSeconds,
    long GenerationTimeLimitSeconds,
    bool PublicView,
    string? UserAgent,
    long? ResponseDelaySeconds);