namespace Honeyslop.Data.Repositories;

public sealed record HoneyslopSettings(
    TimeSpan CacheTtl,
    TimeSpan KeepUnused,
    bool GenerationEnabled,
    TimeSpan RequestRetention,
    TimeSpan GenerationWait,
    bool AllowRefusals,
    TimeSpan ClaimTimeout,
    TimeSpan RetryFailedAfter,
    TimeSpan GenerationTimeLimit,
    bool PublicView)
{
    public static readonly HoneyslopSettings Default = new(
        TimeSpan.Zero,
        TimeSpan.Zero,
        true,
        TimeSpan.FromDays(30),
        TimeSpan.FromMinutes(1),
        true,
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(10),
        false);

    public TimeSpan HeartbeatInterval => ClaimTimeout / 4 > TimeSpan.FromSeconds(5)
        ? ClaimTimeout / 4
        : TimeSpan.FromSeconds(5);
}
