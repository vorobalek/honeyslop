using System.ComponentModel;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Enums;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Admin.Extensions;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Honeyslop.Admin.Mcp.Tools;

[McpServerToolType]
public sealed class Reports(IReportsService reportsService)
{
    [McpServerTool(Name = "list_reports", Title = "List abuse reports", ReadOnly = true, Idempotent = true)]
    [Description("Abuse reports with their state (queued, sending, retrying, sent, failed), recipients, attempts and errors. 50 per page, newest first unless sorted.")]
    public Task<ReportsPageDto> ListAsync(
        [Description("Column to sort by.")] ReportColumn? sort = null,
        [Description("Sort descending.")] bool descending = false,
        [Description("Zero-based page number.")]
        int page = 0,
        CancellationToken cancellationToken = default)
    {
        return reportsService.GetPageAsync(new ReportsQuery(sort.SortedBy(descending), page), cancellationToken);
    }

    [McpServerTool(Name = "get_report", Title = "Get an abuse report", ReadOnly = true, Idempotent = true)]
    [Description("One abuse report with its letter, sender, Message-ID, attachment size, the last error and the bounces that came back for it.")]
    public async Task<ReportDetailsDto> GetAsync([Description("Report id.")] long id, CancellationToken cancellationToken = default)
    {
        return await reportsService.GetAsync(id, cancellationToken) ?? throw new McpException($"There is no report {id}.");
    }

    [McpServerTool(Name = "list_bounced_contacts", Title = "List bounced abuse contacts", ReadOnly = true, Idempotent = true)]
    [Description(
        "Abuse addresses whose mail servers sent our reports back, latest bounce first, 50 per page: how many bounces and reports, the last status code and the server's answer, and whether the address is blocked already. Status 5.x.x is a permanent failure, 4.x.x a delay. block_abuse_contacts stops reporting to an address.")]
    public Task<BouncedContactsPageDto> ListBouncedContactsAsync(
        [Description("Only bounces from the last N days, 0 for all time.")]
        int days = 0,
        [Description("Part of the address or of the server's answer.")]
        string? search = null,
        [Description("Zero-based page number.")]
        int page = 0,
        CancellationToken cancellationToken = default)
    {
        return reportsService.GetBouncedContactsAsync(new BouncedContactsQuery(days, search, page), cancellationToken);
    }
}