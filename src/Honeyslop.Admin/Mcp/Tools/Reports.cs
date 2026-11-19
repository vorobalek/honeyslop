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
    [Description("One abuse report with its letter, sender, Message-ID, attachment size and the last error.")]
    public async Task<ReportDetailsDto> GetAsync([Description("Report id.")] long id, CancellationToken cancellationToken = default)
    {
        return await reportsService.GetAsync(id, cancellationToken) ?? throw new McpException($"There is no report {id}.");
    }
}