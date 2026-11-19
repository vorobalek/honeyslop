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

    public static readonly Counter DelayJobs = Metrics.CreateCounter(
        Constants.MetricsPrefix + "delay_jobs_total",
        "Clients taken from the learning queue, by result.",
        "result");

    public static readonly Counter DelayObservations = Metrics.CreateCounter(
        Constants.MetricsPrefix + "delay_observations_total",
        "Requests whose answer or drop the delay learner took in.");

    public static readonly Gauge DelayJobsWaiting = Metrics.CreateGauge(
        Constants.MetricsPrefix + "delay_jobs_waiting",
        "Clients waiting in the learning queue.");

    public static readonly Gauge PersonalDelays = Metrics.CreateGauge(
        Constants.MetricsPrefix + "client_delays",
        "Clients with a delay of their own, by kind.",
        "kind");
}