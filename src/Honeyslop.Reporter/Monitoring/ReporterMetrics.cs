using Honeyslop.Common;
using Prometheus;

namespace Honeyslop.Reporter.Monitoring;

internal static class ReporterMetrics
{
    public static readonly Gauge Reports = Metrics.CreateGauge(
        Constants.MetricsPrefix + "abuse_reports",
        "Abuse reports in the database, by state; every reporter reports the same numbers.",
        "state");

    public static readonly Gauge Configured = Metrics.CreateGauge(
        Constants.MetricsPrefix + "reporter_configured",
        "1 when SMTP is configured and the reporter takes reports from the queue.");

    public static readonly Gauge BouncedContacts = Metrics.CreateGauge(
        Constants.MetricsPrefix + "abuse_report_bounced_contacts",
        "Abuse addresses that sent a report back, in the database; every reporter reports the same number.");

    public static readonly Gauge MailboxConfigured = Metrics.CreateGauge(
        Constants.MetricsPrefix + "reporter_mailbox_configured",
        "1 when IMAP is configured and the reporter reads bounces from the mailbox.");

    public static readonly Gauge MailboxRead = Metrics.CreateGauge(
        Constants.MetricsPrefix + "reporter_mailbox_read_timestamp_seconds",
        "Unix time the reporter last read every bounce folder without an error.");
}