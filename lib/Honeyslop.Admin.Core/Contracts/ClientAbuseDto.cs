using Honeyslop.Data.Enums;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record ClientAbuseDto(string Ip,
    WhoisDto? Whois,
    ReportDto[] Reports,
    int NewSinceLastSent,
    AbuseDraftDto? Draft,
    string[] BlockedRecipients,
    BouncedContactDto[] Bounces)
{
    public string[] Recipients => [.. (Whois?.AbuseContacts ?? []).Except(BlockedRecipients)];
    public ReportDto? LastReport => Reports.FirstOrDefault();
    public ReportDto? LastSent => Reports.FirstOrDefault(x => x.State == AbuseReportState.Sent);
    public bool ReportPending => Reports.Any(x => x.State is AbuseReportState.Queued or AbuseReportState.Sending or AbuseReportState.Retrying);
    public bool AlreadyReported => LastSent is not null && NewSinceLastSent == 0;
}