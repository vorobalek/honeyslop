using System.ComponentModel;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Enums;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Admin.Extensions;
using Honeyslop.Data.Enums;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Honeyslop.Admin.Mcp.Tools;

[McpServerToolType]
public sealed class Requests(IRequestsService requestsService)
{
    [McpServerTool(Name = "list_requests", Title = "List recorded requests", ReadOnly = true, Idempotent = true)]
    [Description("Requests honeyslop received, as recorded: client address, method, host, path, User-Agent, outcome. 100 per page, newest first unless sorted. Times are UTC.")]
    public Task<RequestsPageDto> ListAsync(
        [Description("Exact client address.")] string? clientIp = null,
        [Description("Part of the host name.")]
        string? host = null,
        [Description("Part of the path and query.")]
        string? path = null,
        [Description("Exact HTTP method.")] string? method = null,
        [Description("Part of the User-Agent.")]
        string? userAgent = null,
        [Description("How the request was answered.")]
        RequestOutcome? outcome = null,
        [Description("Only requests answered with this cached entry.")]
        long? entryId = null,
        [Description("Received at or after this time.")]
        DateTimeOffset? since = null,
        [Description("Received before this time.")]
        DateTimeOffset? until = null,
        [Description("Column to sort by.")] RequestColumn? sort = null,
        [Description("Sort descending.")] bool descending = false,
        [Description("Zero-based page number.")]
        int page = 0,
        CancellationToken cancellationToken = default)
    {
        var filter = new RequestFilter(clientIp, host, path, outcome, entryId, null, method, userAgent, since, until);
        return requestsService.GetPageAsync(new RequestsQuery(filter, sort.SortedBy(descending), page), cancellationToken);
    }

    [McpServerTool(Name = "get_request", Title = "Get a recorded request", ReadOnly = true, Idempotent = true)]
    [Description("One recorded request with its headers and body as received.")]
    public async Task<RequestDetailsDto> GetAsync([Description("Request id.")] long id, CancellationToken cancellationToken = default)
    {
        return await requestsService.GetAsync(id, cancellationToken) ?? throw new McpException($"There is no request {id}.");
    }

    [McpServerTool(Name = "breakdown_requests", Title = "Count requests by a dimension", ReadOnly = true, Idempotent = true)]
    [Description(
        "Counts the requests matching the filter, grouped by one dimension: client, country, host, path, method, User-Agent, outcome, status code, hour or day. Each group has its request count, distinct clients and first and last time. Groups come largest first; hours and days come in time order, the latest ones.")]
    public Task<RequestsBreakdownDto> BreakdownAsync(
        [Description("What to group by.")] BreakdownDimension by,
        [Description("How many groups to return, at most 200.")]
        int top = 20,
        [Description("Exact client address.")] string? clientIp = null,
        [Description("Part of the host name.")]
        string? host = null,
        [Description("Part of the path and query.")]
        string? path = null,
        [Description("Exact HTTP method.")] string? method = null,
        [Description("Part of the User-Agent.")]
        string? userAgent = null,
        [Description("How the request was answered.")]
        RequestOutcome? outcome = null,
        [Description("Received at or after this time.")]
        DateTimeOffset? since = null,
        [Description("Received before this time.")]
        DateTimeOffset? until = null,
        CancellationToken cancellationToken = default)
    {
        var filter = new RequestFilter(clientIp, host, path, outcome, null, null, method, userAgent, since, until);
        return requestsService.GetBreakdownAsync(new RequestsBreakdownQuery(filter, by, top), cancellationToken);
    }
}