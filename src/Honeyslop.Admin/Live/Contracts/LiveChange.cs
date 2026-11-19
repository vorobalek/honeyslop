using Honeyslop.Admin.Live.Enums;

namespace Honeyslop.Admin.Live.Contracts;

public sealed record LiveChange(LiveTopic Topic,
    long? EntryId = null);