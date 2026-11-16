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
            ? await entryRepository.DeleteGeneratedBeforeAsync(now - settings.CacheTtl, cancellationToken)
            : 0;
        var removed = settings.KeepUnused > TimeSpan.Zero
            ? await entryRepository.DeleteUnusedSinceAsync(now - settings.KeepUnused, cancellationToken)
            : 0;
        var pruned = settings.RequestRetention > TimeSpan.Zero
            ? await requestRepository.DeleteReceivedBeforeAsync(now - settings.RequestRetention, cancellationToken)
            : 0;

        if (expired + removed + pruned > 0)
            LogCleaned(logger, expired, removed, pruned);
        return new MaintenanceResult(expired, removed, pruned);
    }

    [LoggerMessage(LogLevel.Information, "Expired {expired} entries past their time to live, removed {removed} unused entries, pruned {pruned} request records")]
    static partial void LogCleaned(ILogger<CacheMaintenance> logger, int expired, int removed, int pruned);
}
