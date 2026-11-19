using System.ComponentModel;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Services;
using ModelContextProtocol.Server;

namespace Honeyslop.Admin.Mcp.Tools;

[McpServerToolType]
public sealed class BlockedContacts(IBlockedContactsService blockedContactsService)
{
    [McpServerTool(Name = "list_blocked_abuse_contacts", Title = "List blocked abuse contacts", ReadOnly = true, Idempotent = true)]
    [Description("Abuse addresses and @domains that never receive reports, newest first, 50 per page, with who blocked them and why.")]
    public Task<BlockedContactsPageDto> ListAsync(
        [Description("Part of the address, domain or note.")]
        string? search = null,
        [Description("Zero-based page number.")]
        int page = 0,
        CancellationToken cancellationToken = default)
    {
        return blockedContactsService.GetPageAsync(new BlockedContactsQuery(search, page), cancellationToken);
    }

    [McpServerTool(Name = "block_abuse_contacts", Title = "Block abuse contacts", Destructive = false, Idempotent = true)]
    [Description(
        "Stops reports to these addresses, or to every address of an @domain. Reports queued for them are not sent either; a report whose every recipient is blocked fails. Already blocked ones are kept as they are. At most 1000 at a time. Returns how many were added, or what was wrong.")]
    public Task<ContactsChangeDto> BlockAsync(
        [Description("Addresses such as abuse@hoster.example or domains such as @hoster.example.")]
        string[] contacts,
        [Description("Why, for example that the hoster answers with bounces.")]
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        return blockedContactsService.BlockAsync(contacts, note, cancellationToken);
    }

    [McpServerTool(Name = "unblock_abuse_contacts", Title = "Unblock abuse contacts", Destructive = false, Idempotent = true)]
    [Description("Lets reports go to these addresses or @domains again. Returns how many were removed.")]
    public Task<ContactsChangeDto> UnblockAsync(
        [Description("Addresses or @domains exactly as blocked.")]
        string[] contacts,
        CancellationToken cancellationToken = default)
    {
        return blockedContactsService.UnblockAsync(contacts, cancellationToken);
    }
}