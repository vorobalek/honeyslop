namespace Honeyslop.Llm.Contracts;

public sealed record LlmServerStatus(
    string? Model,
    long? Parameters,
    long? SizeBytes,
    int Slots,
    int BusySlots,
    int ContextPerSlot,
    long PromptTokens,
    long CachedPromptTokens,
    long PredictedTokens,
    double? PromptTokensPerSecond,
    double? PredictedTokensPerSecond,
    int RequestsProcessing,
    int RequestsDeferred);