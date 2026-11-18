using System.Globalization;
using Honeyslop.Data.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

namespace Honeyslop.Admin.Pages;

public abstract class SortedListModel<TColumn> : PageModel
    where TColumn : struct, Enum
{
    [BindProperty(SupportsGet = true)]
    public TColumn? Sort { get; set; }

    [BindProperty(SupportsGet = true)]
    public SortDirection? Dir { get; set; }

    public Sorting<TColumn>? Sorting => Sort is { } column ? new Sorting<TColumn>(column, Dir ?? SortDirection.Asc) : null;

    public bool IsSorted => Sort is not null;

    public string PreviousLabel => IsSorted ? "← Previous" : "← Newer";

    public string NextLabel => IsSorted ? "Next →" : "Older →";

    public abstract string ListPage { get; }

    public Dictionary<string, string> RouteValues(int pageNumber)
    {
        var values = FilterValues();
        if (Sorting is { } sorting)
        {
            values[nameof(Sort)] = sorting.Column.ToString();
            values[nameof(Dir)] = sorting.Direction.ToString();
        }

        if (pageNumber > 0)
            values["p"] = pageNumber.ToString(CultureInfo.InvariantCulture);
        return values;
    }

    public SortHeader Header(TColumn column, string label, string? cssClass = null)
    {
        SortDirection? current = Sorting is { } sorting && EqualityComparer<TColumn>.Default.Equals(sorting.Column, column)
            ? sorting.Direction
            : null;
        var values = new RouteValueDictionary();
        foreach (var (key, value) in FilterValues())
            values[key] = value;
        if (current != SortDirection.Desc)
        {
            values[nameof(Sort)] = column.ToString();
            values[nameof(Dir)] = (current is null ? SortDirection.Asc : SortDirection.Desc).ToString();
        }

        return new SortHeader(label, Url.Page(ListPage, values) ?? "#", current, cssClass);
    }

    protected abstract Dictionary<string, string> FilterValues();
}
