using Honeyslop.Common;
using Prometheus;

namespace Honeyslop.Reporter.Monitoring;

internal static class ReporterMetrics
{
    public static readonly Counter Reports = Metrics.CreateCounter(
        Constants.MetricsPrefix + "abuse_reports_total",
        "Abuse report sending attempts, by result.",
        "result");

    public static readonly Gauge Configured = Metrics.CreateGauge(
        Constants.MetricsPrefix + "reporter_configured",
        "1 when SMTP is configured and the reporter takes reports from the queue.");

    public static void Initialize()
    {
        foreach (var result in new[] { "sent", "retried", "failed" })
            Reports.WithLabels(result);
    }
}
