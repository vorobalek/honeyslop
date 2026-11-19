namespace Honeyslop.Llm.Contracts;

public sealed record LlmProgress(string Content,
    int Tokens);