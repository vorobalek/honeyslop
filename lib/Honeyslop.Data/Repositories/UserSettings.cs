namespace Honeyslop.Data.Repositories;

public sealed record UserSettings(string? SenderName)
{
    public static UserSettings Default { get; } = new((string?)null);
}
