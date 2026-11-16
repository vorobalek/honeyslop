using Honeyslop.Common;
using Honeyslop.Data;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Migrator;

internal sealed partial class Migrator(
    ApplicationDbContext dbContext,
    IGlobalCancellationTokenSource globalCancellationTokenSource,
    ILogger<Migrator> logger) : IMigrator
{
    public async Task MigrateAsync()
    {
        var cancellationToken = globalCancellationTokenSource.Token;
        if (!dbContext.Database.IsRelational())
            return;

        var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        LogApplying(logger, pending.Length, string.Join(", ", pending));
        await dbContext.Database.MigrateAsync(cancellationToken);
        LogUpToDate(logger);
    }

    [LoggerMessage(LogLevel.Information, "Applying {count} pending migrations: {migrations}")]
    static partial void LogApplying(ILogger<Migrator> logger, int count, string migrations);

    [LoggerMessage(LogLevel.Information, "Database is up to date")]
    static partial void LogUpToDate(ILogger<Migrator> logger);
}
