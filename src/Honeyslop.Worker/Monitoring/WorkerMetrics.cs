using Honeyslop.Common;
using Prometheus;

namespace Honeyslop.Worker.Monitoring;

internal static class WorkerMetrics
{
    public static readonly Gauge Reports = Metrics.CreateGauge(
        Constants.MetricsPrefix + "abuse_reports",
        "Abuse reports in the database, by state; every worker reports the same numbers.",
        "state");

    public static readonly Gauge Configured = Metrics.CreateGauge(
        Constants.MetricsPrefix + "smtp_configured",
        "1 when SMTP is configured and the worker takes abuse reports from the queue.");

    public static readonly Gauge BouncedContacts = Metrics.CreateGauge(
        Constants.MetricsPrefix + "abuse_report_bounced_contacts",
        "Abuse addresses that sent a report back, in the database; every worker reports the same number.");

    public static readonly Gauge MailboxConfigured = Metrics.CreateGauge(
        Constants.MetricsPrefix + "mailbox_configured",
        "1 when IMAP is configured and the worker reads bounces from the mailbox.");

    public static readonly Gauge MailboxRead = Metrics.CreateGauge(
        Constants.MetricsPrefix + "mailbox_read_timestamp_seconds",
        "Unix time the worker last read every bounce folder without an error.");

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

    public static readonly Gauge LlmEndpoints = Metrics.CreateGauge(
        Constants.MetricsPrefix + "llm_endpoints",
        "Model servers found behind the configured address.");

    public static readonly Gauge LlmInFlight = Metrics.CreateGauge(
        Constants.MetricsPrefix + "llm_in_flight",
        "Generations this instance is running on each model server.",
        "endpoint");

    public static readonly Counter LlmBusyWaits = Metrics.CreateCounter(
        Constants.MetricsPrefix + "llm_busy_waits_total",
        "Times a generation waited because every model server slot was busy.");

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
}