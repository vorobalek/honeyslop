using Honeyslop.Data.Repositories;

namespace Honeyslop.Admin.Pages;

public interface IRequestList
{
    string ListPage { get; }
    RequestSummary[] Requests { get; }
    int TotalCount { get; }
    int PageNumber { get; }
    int PageCount { get; }
    DateTimeOffset RenderedAt { get; }
    Dictionary<string, string> RouteValues(int pageNumber);
    SortHeader Header(RequestColumn column, string label, string? cssClass = null);
    string PreviousLabel { get; }
    string NextLabel { get; }
}
