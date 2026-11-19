using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Reports;

namespace Honeyslop.Admin.Core.Services.Internals;

internal static class BounceMapping
{
    extension(AbuseReportBounce bounce)
    {
        public ReportBounceDto ToDto()
        {
            return new ReportBounceDto(bounce.Recipient, bounce.Action, bounce.Status, bounce.Diagnostic, bounce.Mta, bounce.BouncedAt);
        }
    }

    extension(BouncedContact contact)
    {
        public BouncedContactDto ToDto(IReadOnlyCollection<string> blocked)
        {
            return new BouncedContactDto(
                contact.Recipient,
                contact.Bounces,
                contact.Reports,
                contact.LastBouncedAt,
                contact.LastReportId,
                contact.LastStatus,
                contact.LastDiagnostic,
                AbuseContactBlocklist.Blocks(blocked, contact.Recipient));
        }
    }
}