using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;

namespace Honeyslop.Admin.Core.Services.Internals;

internal static class ReportMapping
{
    extension(AbuseReport report)
    {
        public AbuseReportState State => report.Status switch
        {
            AbuseReportStatus.Sent => AbuseReportState.Sent,
            AbuseReportStatus.Failed => AbuseReportState.Failed,
            _ when report.ClaimedBy is not null => AbuseReportState.Sending,
            _ when report.RetryAt is not null => AbuseReportState.Retrying,
            _ => AbuseReportState.Queued
        };

        public ReportDto ToRow(ViewerDto viewer)
        {
            return new ReportDto(
                report.Id,
                report.ClientIp,
                report.Recipients,
                report.Subject,
                report.State,
                viewer.CanOperate ? report.RequestedBy : null,
                report.Attempts,
                report.AttachedRequests,
                report.Error,
                report.CreatedAt,
                report.SentAt,
                report.RetryAt);
        }
    }
}