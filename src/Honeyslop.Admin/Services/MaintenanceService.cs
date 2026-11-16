using Honeyslop.Admin.Options;
using Honeyslop.Data.Repositories;
using Microsoft.Extensions.Options;

namespace Honeyslop.Admin.Services;

internal sealed partial class MaintenanceService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<MaintenanceOptions> options,
    TimeProvider timeProvider,
    ILogger<MaintenanceService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval, timeProvider);
        try
        {
            do
            {
                await RunAsync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var asyncScope = serviceScopeFactory.CreateAsyncScope();
            var settings = await asyncScope.ServiceProvider.GetRequiredService<ISettingsRepository>().GetAsync(cancellationToken);
            await asyncScope.ServiceProvider.GetRequiredService<ICacheMaintenance>().RunAsync(settings, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogFailed(logger, exception);
        }
    }

    [LoggerMessage(LogLevel.Warning, "Cache maintenance failed")]
    static partial void LogFailed(ILogger<MaintenanceService> logger, Exception exception);
}
