namespace Honeyslop.Llm.Contracts;

public sealed record LlmEndpoint(string Name,
    Uri Url);