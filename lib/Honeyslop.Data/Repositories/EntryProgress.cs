namespace Honeyslop.Data.Repositories;

public sealed record EntryProgress(
    long Id,
    string Method,
    string Host,
    string Target,
    int Attempts,
    string? ClaimedBy,
    DateTimeOffset? ClaimedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RequestedAt,
    string? Generator);
