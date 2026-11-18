using Honeyslop.Data.Repositories;
using Prometheus;

namespace Honeyslop.Reporter.Monitoring;

internal sealed partial class ReportStateMetrics(
    IServiceScopeFactory scopeFactory,
    ILogger<ReportStateMetrics> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Metrics.DefaultRegistry.AddBeforeCollectCallback(CollectAsync);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task CollectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var counts = await scope.ServiceProvider
                .GetRequiredService<IAbuseReportRepository>()
                .CountByStateAsync(cancellationToken);
            foreach (var (state, count) in counts)
                ReporterMetrics.Reports.WithLabels(state.ToString().ToLowerInvariant()).Set(count);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogCountFailed(logger, exception);
        }
    }

    [LoggerMessage(LogLevel.Warning, "Could not count the abuse reports for the metrics, keeping the last numbers")]
    static partial void LogCountFailed(ILogger<ReportStateMetrics> logger, Exception exception);
}
