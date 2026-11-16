namespace Honeyslop.Data.Repositories;

public sealed record GeneratorStatistics(
    string Model,
    int Generated,
    double AverageMs,
    double AverageTokens,
    DateTimeOffset? LastGeneratedAt);
