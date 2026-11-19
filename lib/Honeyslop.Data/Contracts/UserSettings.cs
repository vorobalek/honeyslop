namespace Honeyslop.Data.Contracts;

public sealed record UserSettings
{
    public string UserId { get; init; } = null!;
    public string? SenderName { get; init; }
}