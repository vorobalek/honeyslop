using System.Text.Json.Nodes;

namespace Honeyslop.Llm;

internal sealed record ChatCompletionRequest(
    string Model,
    IReadOnlyList<ChatMessage> Messages,
    double Temperature,
    int MaxTokens,
    ChatResponseFormat ResponseFormat,
    bool Stream);

internal sealed record ChatMessage(string Role, string? Content);

internal sealed record ChatResponseFormat(string Type, ChatJsonSchema JsonSchema);

internal sealed record ChatJsonSchema(string Name, bool Strict, JsonNode Schema);

internal sealed record ChatCompletionResponse(
    string? Model,
    IReadOnlyList<ChatChoice>? Choices,
    ChatUsage? Usage);

internal sealed record ChatChoice(ChatMessage? Message, string? FinishReason);

internal sealed record ChatUsage(int? PromptTokens, int? CompletionTokens);
