using Honeyslop.Admin.Core.Enums;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record EntriesQuery(EntryFilter Filter,
    Sorting<EntryColumn>? Sort = null,
    int Page = 0);