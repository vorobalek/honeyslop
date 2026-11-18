using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record EntriesPageDto(EntryTotals Totals, PagedDto<Entry> Entries, DateTimeOffset AsOf);
