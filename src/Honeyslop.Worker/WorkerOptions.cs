namespace Honeyslop.Worker;

public sealed class WorkerOptions
{
    public string InstanceName { get; set; } = null!;
    public int GenerationWorkers { get; set; }
    public int MaxAttempts { get; set; }
}