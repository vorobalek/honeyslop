using Honeyslop.Data.Entities;

namespace Honeyslop.Data.Repositories;

public sealed record EntryResponse(
    int Status,
    IReadOnlyList<ResponseHeader> Headers,
    string Body,
    string? Model,
    string? Generator,
    int? PromptTokens,
    int? CompletionTokens,
    int GenerationMs);
