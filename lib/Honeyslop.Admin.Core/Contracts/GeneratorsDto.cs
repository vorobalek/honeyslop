using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record GeneratorsDto(
    bool Configured,
    GeneratorDto[] Generators,
    EntryTotals Totals,
    DateTimeOffset? OldestQueuedAt,
    Entry[] Generating,
    Entry[] Queued,
    DateTimeOffset AsOf)
{
    public int TotalSlots => Generators.Sum(x => x.Status?.Slots ?? 0);
    public int BusySlots => Generators.Sum(x => x.Status?.BusySlots ?? 0);
}