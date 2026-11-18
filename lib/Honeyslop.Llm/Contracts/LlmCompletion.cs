namespace Honeyslop.Llm.Contracts;

public sealed record LlmCompletion(
    string Content,
    string? FinishReason,
    string? Model,
    int? PromptTokens,
    int? CompletionTokens,
    string? Endpoint = null);
