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
}