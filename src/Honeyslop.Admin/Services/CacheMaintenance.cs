using Honeyslop.Data.Repositories;

namespace Honeyslop.Admin.Services;

internal sealed partial class CacheMaintenance(
    IEntryRepository entryRepository,
    IRequestRepository requestRepository,
    TimeProvider timeProvider,
    ILogger<CacheMaintenance> logger) : ICacheMaintenance
{
    public async Task<MaintenanceResult> RunAsync(HoneyslopSettings settings, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var expired = settings.CacheTtl > TimeSpan.Zero
            ? await entryRepository.ExpireGeneratedBeforeAsync(now - settings.CacheTtl, now, cancellationToken)
            : 0;
        var unused = settings.KeepUnused > TimeSpan.Zero
            ? await entryRepository.ExpireUnusedSinceAsync(now - settings.KeepUnused, now, cancellationToken)
            : 0;
        var pruned = settings.RequestRetention > TimeSpan.Zero
            ? await requestRepository.DeleteReceivedBeforeAsync(now - settings.RequestRetention, cancellationToken)
            : 0;
        var deleted = await entryRepository.DeleteExpiredUnreferencedAsync(now - GeneratorStatistics.Window, cancellationToken);

        if (expired + unused + pruned + deleted > 0)
            LogCleaned(logger, expired, unused, pruned, deleted);
        return new MaintenanceResult(expired, unused, pruned, deleted);
    }

    [LoggerMessage(LogLevel.Information, "Expired {expired} entries past their time to live and {unused} unused entries, pruned {pruned} request records, deleted {deleted} expired entries no request refers to")]
    static partial void LogCleaned(ILogger<CacheMaintenance> logger, int expired, int unused, int pruned, int deleted);
}
