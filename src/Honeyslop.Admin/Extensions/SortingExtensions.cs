using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Data.Enums;

namespace Honeyslop.Admin.Extensions;

public static class SortingExtensions
{
    extension<TColumn>(TColumn? column)
        where TColumn : struct, Enum
    {
        public Sorting<TColumn>? SortedBy(bool descending)
        {
            return column is { } value
                ? new Sorting<TColumn>(value, descending ? SortDirection.Desc : SortDirection.Asc)
                : null;
        }
    }
}
