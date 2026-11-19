using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Data.Contracts;

namespace Honeyslop.Admin.Views.Contracts;

public sealed record EntriesViewDto(EntryTotals Totals,
    PagedDto<EntryRowDto> Entries,
    DateTimeOffset AsOf);