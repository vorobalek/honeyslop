using System.ComponentModel;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Enums;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Admin.Extensions;
using Honeyslop.Data.Entities;
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
        [Description("Part of the host name.")]
        string? host = null,
        [Description("Part of the path and query.")]
        string? path = null,
        [Description("Only entries in this state.")]
        EntryState? state = null,
        [Description("Column to sort by.")] EntryColumn? sort = null,
        [Description("Sort descending.")] bool descending = false,
        [Description("Zero-based page number.")]
        int page = 0,
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
        [Description("Part of the host name.")]
        string? host = null,
        [Description("Part of the path and query.")]
        string? path = null,
        [Description("Only entries in this state.")]
        EntryState? state = null,
        [Description("Confirms expiring every entry when no filter is given.")]
        bool everything = false,
        CancellationToken cancellationToken = default)
    {
        var filter = new EntryFilter(host, path, state);
        if (filter.IsEmpty && !everything)
            throw new McpException("No filter given: pass everything=true to expire every entry.");
        return entriesService.ExpireMatchingAsync(filter, cancellationToken);
    }

    [McpServerTool(Name = "regenerate_entries", Title = "Regenerate cached responses", Destructive = true, Idempotent = false)]
    [Description(
        "Expires these cached responses and queues each path to be generated again, optionally with guidance for the model such as the software to answer as. An expired entry is changed only when its path has no newer entry. Returns the ids of the new entries, or what was wrong.")]
    public Task<EntryChangeDto> RegenerateAsync(
        [Description("Entry ids.")] long[] ids,
        [Description("Guidance for the model, at most 2048 characters.")]
        string? guidance = null,
        CancellationToken cancellationToken = default)
    {
        return entriesService.RegenerateAsync(ids, guidance, cancellationToken);
    }

    [McpServerTool(Name = "replace_entries", Title = "Write cached responses", Destructive = true, Idempotent = false)]
    [Description(
        "Expires these cached responses and serves the given response for each path instead. Headers the server sets itself, such as Content-Length, are refused; Content-Type defaults to text/html. An expired entry is changed only when its path has no newer entry. Returns the ids of the new entries, or what was wrong.")]
    public Task<EntryChangeDto> ReplaceAsync(
        [Description("Entry ids.")] long[] ids,
        [Description("HTTP status, 200 to 599.")]
        int status,
        [Description("Response body.")] string body,
        [Description("Response headers by name.")]
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        ResponseHeader[] list =
        [
            .. (headers ?? []).Select(x => new ResponseHeader
            {
                Name = x.Key,
                Value = x.Value
            })
        ];
        return entriesService.ReplaceAsync(ids, new EntryResponseDto(status, list, body), cancellationToken);
    }
}