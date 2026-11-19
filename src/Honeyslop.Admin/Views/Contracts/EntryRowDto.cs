using Honeyslop.Data.Enums;

namespace Honeyslop.Admin.Views.Contracts;

public sealed record EntryRowDto(
    long Id,
    EntryState State,
    string Method,
    string Host,
    string Target,
    int? ResponseStatus,
    int BodyLength,
    long Hits,
    int Attempts,
    string? Generator,
    string? Error,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClaimedAt,
    DateTimeOffset? RequestedAt,
    DateTimeOffset? LastHitAt);
