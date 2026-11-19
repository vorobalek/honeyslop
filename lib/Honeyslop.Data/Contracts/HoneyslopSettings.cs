namespace Honeyslop.Data.Contracts;

public sealed record HoneyslopSettings
{
    public static HoneyslopSettings Default { get; } = new();

    public TimeSpan CacheTtl { get; init; } = TimeSpan.Zero;
    public TimeSpan KeepUnused { get; init; } = TimeSpan.Zero;
    public bool GenerationEnabled { get; init; } = true;
    public TimeSpan RequestRetention { get; init; } = TimeSpan.FromDays(30);
    public TimeSpan GenerationWait { get; init; } = TimeSpan.FromMinutes(1);
    public bool AllowRefusals { get; init; } = true;
    public TimeSpan ClaimTimeout { get; init; } = TimeSpan.FromMinutes(2);
    public TimeSpan RetryFailedAfter { get; init; } = TimeSpan.FromMinutes(1);
    public TimeSpan GenerationTimeLimit { get; init; } = TimeSpan.FromMinutes(10);
    public bool PublicView { get; init; }
    public string? UserAgent { get; init; }

    public TimeSpan HeartbeatInterval => ClaimTimeout / 4 > TimeSpan.FromSeconds(5)
        ? ClaimTimeout / 4
        : TimeSpan.FromSeconds(5);
}