namespace Honeyslop.Data.Contracts;

public sealed record GeneratorActivity
{
    public string Generator { get; init; } = null!;
    public int Generated { get; init; }
    public double AverageMs { get; init; }
    public double AverageTokens { get; init; }
    public DateTimeOffset? LastGeneratedAt { get; init; }
}