using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Repositories;

namespace Honeyslop.Admin.Core.Maintenance.Internals;

internal sealed partial class CacheMaintenance(
    IEntryRepository entryRepository,
    IRequestRepository requestRepository,
    IServerTimeRepository serverTime,
    ILogger<CacheMaintenance> logger) : ICacheMaintenance
{
    public async Task<MaintenanceResultDto> RunAsync(HoneyslopSettings settings, CancellationToken cancellationToken)
    {
        var now = await serverTime.GetNowAsync(cancellationToken);
        var expired = settings.CacheTtl > TimeSpan.Zero
            ? await entryRepository.ExpireGeneratedBeforeAsync(now - settings.CacheTtl, now, cancellationToken)
            : 0;
        var unused = settings.KeepUnused > TimeSpan.Zero
            ? await entryRepository.ExpireUnusedSinceAsync(now - settings.KeepUnused, now, cancellationToken)
            : 0;
        var pruned = settings.RequestRetention > TimeSpan.Zero
            ? await requestRepository.DeleteReceivedBeforeAsync(now - settings.RequestRetention, cancellationToken)
            : 0;
        var deleted = await entryRepository.DeleteExpiredUnreferencedAsync(now - IGeneratorsService.ProductionWindow, cancellationToken);

        if (expired + unused + pruned + deleted > 0)
            LogCleaned(logger, expired, unused, pruned, deleted);
        return new MaintenanceResultDto(expired, unused, pruned, deleted);
    }

    [LoggerMessage(LogLevel.Information, "Expired {expired} entries past their time to live and {unused} unused entries, pruned {pruned} request records, deleted {deleted} expired entries no request refers to")]
    static partial void LogCleaned(ILogger<CacheMaintenance> logger, int expired, int unused, int pruned, int deleted);
}
