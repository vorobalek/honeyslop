using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Data.Contracts;

namespace Honeyslop.Admin.Views.Contracts;

public sealed record GeneratorsViewDto(
    bool Configured,
    GeneratorDto[] Generators,
    EntryTotals Totals,
    DateTimeOffset? OldestQueuedAt,
    EntryRowDto[] Generating,
    EntryRowDto[] Queued,
    int TotalSlots,
    int BusySlots,
    DateTimeOffset AsOf);