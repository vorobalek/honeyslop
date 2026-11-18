using Honeyslop.Data.Enums;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record Sorting<TColumn>(TColumn Column, SortDirection Direction)
    where TColumn : struct, Enum;
