namespace Honeyslop.Data.Contracts;

public sealed record Settings
{
    public HoneyslopSettings Honeyslop { get; init; } = HoneyslopSettings.Default;
    public UserSettings? User { get; init; }
}