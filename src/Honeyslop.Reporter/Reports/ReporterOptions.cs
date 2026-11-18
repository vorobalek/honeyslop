namespace Honeyslop.Reporter.Reports;

public sealed class ReporterOptions
{
    public string InstanceName { get; set; } = null!;
    public int MaxAttachedRequests { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan LeaseTimeout { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan[] RetryDelays { get; set; } =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1)
    ];
}
