using System.Text.Json.Nodes;

namespace Honeyslop.Llm.Contracts;

internal sealed record ChatCompletionRequest(
    string Model,
    IReadOnlyList<ChatMessage> Messages,
    double Temperature,
    int MaxTokens,
    ChatResponseFormat ResponseFormat,
    bool Stream,
    ChatStreamOptions StreamOptions);

internal sealed record ChatMessage(string Role,
    string? Content);

internal sealed record ChatResponseFormat(string Type,
    ChatJsonSchema JsonSchema);

internal sealed record ChatJsonSchema(string Name,
    bool Strict,
    JsonNode Schema);

internal sealed record ChatStreamOptions(bool IncludeUsage);

internal sealed record ChatCompletionChunk(
    string? Model,
    IReadOnlyList<ChatChunkChoice>? Choices,
    ChatUsage? Usage,
    JsonNode? Error);

internal sealed record ChatChunkChoice(ChatMessage? Delta,
    string? FinishReason);

internal sealed record ChatUsage(int? PromptTokens,
    int? CompletionTokens);