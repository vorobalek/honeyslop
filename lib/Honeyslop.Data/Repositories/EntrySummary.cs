using Honeyslop.Data.Enums;

namespace Honeyslop.Data.Repositories;

public sealed record EntrySummary(
    long Id,
    EntryStatus Status,
    string Method,
    string Host,
    string Target,
    int? ResponseStatus,
    int BodyLength,
    long Hits,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastHitAt,
    string? Error,
    string? ClaimedBy,
    DateTimeOffset? ClaimedAt)
{
    public EntryState State => Status switch
    {
        EntryStatus.Pending when ClaimedBy is null => EntryState.Queued,
        EntryStatus.Pending => EntryState.Generating,
        EntryStatus.Ready => EntryState.Ready,
        _ => EntryState.Failed
    };
}
