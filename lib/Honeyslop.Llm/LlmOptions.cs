namespace Honeyslop.Llm;

public class LlmOptions
{
    public IReadOnlyList<Uri> Urls { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int MaxTokens { get; set; }
    public double Temperature { get; set; }
    public double TopP { get; set; } = 0.8;
    public int TopK { get; set; } = 20;
    public double MinP { get; set; }
    public TimeSpan Timeout { get; set; }
    public TimeSpan DiscoveryInterval { get; set; }
    public TimeSpan SlotPollInterval { get; set; }
    public TimeSpan ProbeTimeout { get; set; }
    public TimeSpan UnhealthyCooldown { get; set; }
    public TimeSpan DispatchGrace { get; set; }
}