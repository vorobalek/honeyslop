using System.ComponentModel;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Enums;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Admin.Extensions;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Honeyslop.Admin.Mcp.Tools;

[McpServerToolType]
public sealed class Clients(IClientsService clientsService)
{
    [McpServerTool(Name = "list_clients", Title = "List client addresses", ReadOnly = true, Idempotent = true)]
    [Description("Addresses that sent requests within the period, with request, host and path counts, first and last request and the last User-Agent. 50 per page, most recent first unless sorted.")]
    public Task<ClientsPageDto> ListAsync(
        [Description("Seen within this many days; 0 for all history.")]
        int days = 7,
        [Description("Column to sort by.")] ClientColumn? sort = null,
        [Description("Sort descending.")] bool descending = false,
        [Description("Zero-based page number.")]
        int page = 0,
        CancellationToken cancellationToken = default)
    {
        return clientsService.GetPageAsync(new ClientsQuery(days, sort.SortedBy(descending), page), cancellationToken);
    }

    [McpServerTool(Name = "get_client", Title = "Get a client address", ReadOnly = true, Idempotent = true)]
    [Description("One client address: country, request, host and path counts, first and last request, the last User-Agent and the response delay set on it.")]
    public async Task<ClientDto> GetAsync([Description("Client address.")] string ip, CancellationToken cancellationToken = default)
    {
        return await clientsService.GetAsync(ip, cancellationToken) ?? throw new McpException($"{ip} never sent anything.");
    }

    [McpServerTool(Name = "get_client_abuse", Title = "Get the abuse status of an address", ReadOnly = true, Idempotent = true, OpenWorld = true)]
    [Description(
        "Abuse status of a client address: its network from RIPEstat (prefix, origin AS, owner, abuse contacts, registry records), the abuse reports about it, how many requests arrived since the last sent report, and the letter a new report would carry.")]
    public async Task<ClientAbuseDto> GetAbuseAsync([Description("Client address.")] string ip, CancellationToken cancellationToken = default)
    {
        return await clientsService.GetAbuseAsync(ip, cancellationToken) ?? throw new McpException($"{ip} never sent anything.");
    }

    [McpServerTool(Name = "set_client_delay", Title = "Delay the responses to a client", Destructive = false, Idempotent = true)]
    [Description(
        "Makes the trap answer the address no sooner than this many milliseconds after a request arrives when it serves the response from an entry, whatever delay the entry or the settings ask for, to hold a scanner's connections open longer; time spent generating counts towards it. Without milliseconds the address goes back to the entries' delays and the default. Returns the delay as saved, or what was wrong.")]
    public Task<ClientDelayChangeDto> SetDelayAsync(
        [Description("Client address.")] string ip,
        [Description("Milliseconds from 0 to 600000; leave out to clear.")]
        int? milliseconds = null,
        CancellationToken cancellationToken = default)
    {
        return clientsService.SetResponseDelayAsync(ip, milliseconds, cancellationToken);
    }

    [McpServerTool(Name = "reset_client_delays", Title = "Remove every delay set on a client by hand", Destructive = true, Idempotent = true)]
    [Description("Removes the delay set by hand on every client address; learned delays and the defaults apply to them again. Returns how many were removed.")]
    public Task<ClientDelaysChangeDto> ResetDelaysAsync(CancellationToken cancellationToken = default)
    {
        return clientsService.ResetResponseDelaysAsync(cancellationToken);
    }

    [McpServerTool(Name = "reset_learned_delays", Title = "Forget every learned client delay", Destructive = true, Idempotent = true)]
    [Description("Forgets what Auto-Delay learned about every client, so each starts again from the middle of the range. Returns how many were forgotten.")]
    public Task<ClientDelaysChangeDto> ResetLearnedDelaysAsync(CancellationToken cancellationToken = default)
    {
        return clientsService.ResetLearnedDelaysAsync(cancellationToken);
    }

    [McpServerTool(Name = "send_abuse_report", Title = "Send an abuse report", Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description(
        "Queues an abuse report about the address to the abuse contacts RIPEstat publishes for its network; the reporter mails it within seconds with every recorded request attached, and it cannot be taken back. Refused while a report about the address is on its way, when nothing new arrived since the last sent one, and when the network publishes no abuse contact.")]
    public Task<ReportRequestDto> ReportAsync(
        [Description("Client address.")] string ip,
        [Description("Name to sign the letter with; the operator's saved name when omitted.")]
        string? senderName = null,
        CancellationToken cancellationToken = default)
    {
        return clientsService.ReportAsync(ip, senderName, cancellationToken);
    }
}