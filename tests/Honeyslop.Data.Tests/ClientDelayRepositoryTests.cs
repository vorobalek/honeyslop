using Honeyslop.Data.Entities;
using Honeyslop.Data.Repositories;
using Honeyslop.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class ClientDelayRepositoryTests
{
    [TestMethod]
    public async Task SetAsync_AddsChangesAndClearsTheDelayOfAClient()
    {
        var ip = $"198.51.100.{Random.Shared.Next(1, 254)}-{Unique.String(6)}";
        var now = DateTimeOffset.UtcNow;

        await SetAsync(ip, 1500, "operator", now);
        var added = await GetAsync(ip);
        await SetAsync(ip, 30_000, "agent", now.AddMinutes(1));
        var changed = await GetAsync(ip);
        string[] all;
        await using (var scope = PostgresDatabase.CreateScope())
        {
            all = [.. (await Repository(scope).GetAllAsync(CancellationToken.None)).Select(x => x.ClientIp)];
        }

        await SetAsync(ip, null, "operator", now.AddMinutes(2));

        Assert.Contains(ip, all);
        Assert.AreEqual((1500, "operator"), (added!.ResponseDelayMilliseconds, added.ChangedBy));
        Assert.AreEqual((30_000, "agent"), (changed!.ResponseDelayMilliseconds, changed.ChangedBy));
        Assert.IsNull(await GetAsync(ip));
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task ClearAsync_RemovesEveryManualDelay()
    {
        var ip = $"198.51.100.{Random.Shared.Next(1, 254)}-{Unique.String(6)}";
        await SetAsync(ip, 7900, "operator", DateTimeOffset.UtcNow);

        int cleared;
        await using (var scope = PostgresDatabase.CreateScope())
        {
            cleared = await Repository(scope).ClearAsync(CancellationToken.None);
        }

        Assert.IsGreaterThanOrEqualTo(1, cleared);
        Assert.IsNull(await GetAsync(ip));
    }

    private static async Task SetAsync(string ip, int? milliseconds, string changedBy, DateTimeOffset now)
    {
        await using var scope = PostgresDatabase.CreateScope();
        await Repository(scope).SetAsync(ip, milliseconds, changedBy, now, CancellationToken.None);
    }

    private static async Task<ClientDelay?> GetAsync(string ip)
    {
        await using var scope = PostgresDatabase.CreateScope();
        return await Repository(scope).GetOrDefaultAsync(ip, CancellationToken.None);
    }

    private static IClientDelayRepository Repository(AsyncServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<IClientDelayRepository>();
    }
}