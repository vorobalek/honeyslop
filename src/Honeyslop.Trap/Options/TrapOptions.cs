using Honeyslop.Data.Requests;

namespace Honeyslop.Trap.Options;

public class TrapOptions
{
    public string InstanceName { get; set; } = null!;
    public int MaxWaiters { get; set; }
    public int MaxAttempts { get; set; }
    public int MaxRequestBodyBytes { get; set; }
    public TimeSpan MemoryCacheDuration { get; set; }
    public long MemoryCacheSizeLimit { get; set; }
    public TimeSpan HitFlushInterval { get; set; }
    public TimeSpan ResponseDelay { get; set; }
    public string NoiseDirectory { get; set; } = null!;
    public RequestRules Requests { get; set; } = new();
}