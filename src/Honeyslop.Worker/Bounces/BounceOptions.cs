namespace Honeyslop.Worker.Bounces;

public sealed class BounceOptions
{
    public string[] Folders { get; set; } = ["INBOX"];
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(5);
    public int BatchSize { get; set; } = 50;
}