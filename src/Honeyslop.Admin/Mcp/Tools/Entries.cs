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
public sealed class Entries(IEntriesService entriesService)
{
    [McpServerTool(Name = "list_entries", Title = "List cached responses", ReadOnly = true, Idempotent = true)]
    [Description("Cached responses honeyslop generated, one per method + host + path, with counts by state. 50 per page, newest first unless sorted.")]
    public Task<EntriesPageDto> ListAsync(
        [Description("Part of the host name.")] string? host = null,
        [Description("Part of the path and query.")] string? path = null,
        [Description("Only entries in this state.")] EntryState? state = null,
        [Description("Column to sort by.")] EntryColumn? sort = null,
        [Description("Sort descending.")] bool descending = false,
        [Description("Zero-based page number.")] int page = 0,
        CancellationToken cancellationToken = default)
    {
        return entriesService.GetPageAsync(new EntriesQuery(new EntryFilter(host, path, state), sort.SortedBy(descending), page), cancellationToken);
    }

    [McpServerTool(Name = "get_entry", Title = "Get a cached response", ReadOnly = true, Idempotent = true)]
    [Description("One cached response: the request that triggered it, the generated status, headers and body, and how it was generated.")]
    public async Task<EntryDetailsDto> GetAsync([Description("Entry id.")] long id, CancellationToken cancellationToken = default)
    {
        return await entriesService.GetAsync(id, cancellationToken) ?? throw new McpException($"There is no entry {id}.");
    }

    [McpServerTool(Name = "expire_entries", Title = "Expire cached responses", Destructive = true, Idempotent = true)]
    [Description("Stops serving these cached responses; the next request for each path is generated again. Returns how many were expired.")]
    public Task<int> ExpireAsync([Description("Entry ids.")] long[] ids, CancellationToken cancellationToken = default)
    {
        return entriesService.ExpireAsync(ids, cancellationToken);
    }

    [McpServerTool(Name = "expire_matching_entries", Title = "Expire cached responses by filter", Destructive = true, Idempotent = true)]
    [Description("Expires every cached response matching the filter. Without any filter it expires everything, and then everything=true is required. Returns how many were expired.")]
    public Task<int> ExpireMatchingAsync(
        [Description("Part of the host name.")] string? host = null,
        [Description("Part of the path and query.")] string? path = null,
        [Description("Only entries in this state.")] EntryState? state = null,
        [Description("Confirms expiring every entry when no filter is given.")] bool everything = false,
        CancellationToken cancellationToken = default)
    {
        var filter = new EntryFilter(host, path, state);
        if (filter.IsEmpty && !everything)
            throw new McpException("No filter given: pass everything=true to expire every entry.");
        return entriesService.ExpireMatchingAsync(filter, cancellationToken);
    }
}
