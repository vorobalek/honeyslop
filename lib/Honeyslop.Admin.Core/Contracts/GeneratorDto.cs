using Honeyslop.Llm.Contracts;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record GeneratorDto(
    string Name,
    string? Address,
    LlmServerStatus? Status,
    bool Discovered,
    int Generated,
    double AverageMs,
    double AverageTokens,
    DateTimeOffset? LastGeneratedAt);
