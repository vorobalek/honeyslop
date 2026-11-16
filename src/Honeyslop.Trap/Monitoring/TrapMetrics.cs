using Honeyslop.Common;
using Prometheus;

namespace Honeyslop.Trap.Monitoring;

internal static class TrapMetrics
{
    public static readonly Counter Requests = Metrics.CreateCounter(
        Constants.MetricsPrefix + "trap_requests_total",
        "Requests answered by the trap, by outcome.",
        "outcome");

    public static readonly Counter Generations = Metrics.CreateCounter(
        Constants.MetricsPrefix + "generations_total",
        "Finished generations, by result.",
        "result");

    public static readonly Histogram GenerationDuration = Metrics.CreateHistogram(
        Constants.MetricsPrefix + "generation_duration_seconds",
        "Time spent on one generation, including the wait in the model server queue.",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(1, 2, 11)
        });

    public static readonly Counter CompletionTokens = Metrics.CreateCounter(
        Constants.MetricsPrefix + "generation_completion_tokens_total",
        "Tokens produced by the model.");

    public static readonly Gauge Waiters = Metrics.CreateGauge(
        Constants.MetricsPrefix + "waiters",
        "Requests held open while their response is being generated.");

    public static readonly Gauge LlmEndpoints = Metrics.CreateGauge(
        Constants.MetricsPrefix + "llm_endpoints",
        "Model servers found behind the configured address.");

    public static readonly Gauge LlmInFlight = Metrics.CreateGauge(
        Constants.MetricsPrefix + "llm_in_flight",
        "Generations this instance is running on each model server.",
        "endpoint");

    public static readonly Counter RequestLogWritten = Metrics.CreateCounter(
        Constants.MetricsPrefix + "request_log_written_total",
        "Request records stored in the database.");

    public static readonly Counter RequestLogDropped = Metrics.CreateCounter(
        Constants.MetricsPrefix + "request_log_dropped_total",
        "Request records lost because the queue was full or the database write failed.");

    public static readonly Counter LlmBusyWaits = Metrics.CreateCounter(
        Constants.MetricsPrefix + "llm_busy_waits_total",
        "Times a generation waited because every model server slot was busy.");
}
