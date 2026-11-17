namespace Honeyslop.Data.Repositories;

public sealed record GeneratorStatistics(
    string Generator,
    int Generated,
    double AverageMs,
    double AverageTokens,
    DateTimeOffset? LastGeneratedAt)
{
    public static readonly TimeSpan Window = TimeSpan.FromHours(24);
}
