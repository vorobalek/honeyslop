namespace Honeyslop.Llm.Contracts;

public sealed record GenerationOutputDto(
    long EntryId,
    int Tokens,
    string Content,
    bool Truncated = false);
