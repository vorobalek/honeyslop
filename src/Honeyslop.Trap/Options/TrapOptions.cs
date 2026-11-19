namespace Honeyslop.Trap.Options;

public class TrapOptions
{
    public string InstanceName { get; set; } = null!;
    public int Workers { get; set; }
    public int MaxWaiters { get; set; }
    public int MaxAttempts { get; set; }
    public int MaxTargetLength { get; set; }
    public int MaxRequestBodyBytes { get; set; }
    public TimeSpan MemoryCacheDuration { get; set; }
    public long MemoryCacheSizeLimit { get; set; }
    public TimeSpan HitFlushInterval { get; set; }
    public IReadOnlySet<string> StaticExtensions { get; set; } = null!;
}