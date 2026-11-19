using Honeyslop.Common;
using Prometheus;

namespace Honeyslop.Trap.Monitoring;

internal static class TrapMetrics
{
    public static readonly Counter Requests = Metrics.CreateCounter(
        Constants.MetricsPrefix + "trap_requests_total",
        "Requests answered by the trap, by outcome.",
        "outcome");

    public static readonly Gauge Waiters = Metrics.CreateGauge(
        Constants.MetricsPrefix + "waiters",
        "Requests held open while their response is being generated.");

    public static readonly Counter RequestLogWritten = Metrics.CreateCounter(
        Constants.MetricsPrefix + "request_log_written_total",
        "Request records stored in the database.");

    public static readonly Counter RequestLogDropped = Metrics.CreateCounter(
        Constants.MetricsPrefix + "request_log_dropped_total",
        "Request records lost because the queue was full or the database write failed.");

    public static readonly Histogram RequestDuration = Metrics.CreateHistogram(
        Constants.MetricsPrefix + "trap_request_duration_seconds",
        "Time from a request to its last byte or to the client giving up, by outcome.",
        new HistogramConfiguration
        {
            LabelNames = ["outcome"],
            Buckets = Histogram.ExponentialBuckets(0.25, 2, 12)
        });

    public static readonly Counter ResponseDelays = Metrics.CreateCounter(
        Constants.MetricsPrefix + "response_delays_total",
        "Answers held back, by where their delay came from.",
        "source");

    public static readonly Gauge PersonalDelays = Metrics.CreateGauge(
        Constants.MetricsPrefix + "client_delays",
        "Clients with a delay of their own, by kind.",
        "kind");
}