using Honeyslop.Admin.Live.Enums;

namespace Honeyslop.Admin.Live.Contracts;

public sealed record LiveInterest(LiveTopic[] Topics, long? EntryId = null, bool Periodic = false)
{
    public static readonly LiveInterest Nothing = new([]);

    public bool Matches(LiveChange change)
    {
        return Topics.Contains(change.Topic) && (EntryId is null || change.EntryId is null || change.EntryId == EntryId);
    }
}
