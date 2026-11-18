using Honeyslop.Data.Repositories;

namespace Honeyslop.Admin.Pages;

public sealed record SortHeader(string Label, string Href, SortDirection? Current, string? CssClass)
{
    public string StateClass => Current switch
    {
        SortDirection.Asc => "sorted-asc",
        SortDirection.Desc => "sorted-desc",
        _ => string.Empty
    };

    public string AriaSort => Current switch
    {
        SortDirection.Asc => "ascending",
        SortDirection.Desc => "descending",
        _ => "none"
    };
}
