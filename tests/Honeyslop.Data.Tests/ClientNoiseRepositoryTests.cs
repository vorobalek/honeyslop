using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class ClientNoiseRepositoryTests
{
    [TestMethod]
    public async Task SetAsync_AddsChangesAndClearsTheNoiseOfAClient()
    {
        var ip = $"198.51.100.{Random.Shared.Next(1, 254)}-{Unique.String(6)}";
        var now = DateTimeOffset.UtcNow;

        await SetAsync(ip, NoiseTier.Tier1, "operator", now);
        var added = await GetAsync(ip);
        await SetAsync(ip, NoiseTier.Tier3, "agent", now.AddMinutes(1));
        var changed = await GetAsync(ip);
        string[] all;
        await using (var scope = PostgresDatabase.CreateScope())
        {
            all = [.. (await Repository(scope).GetAllAsync(CancellationToken.None)).Select(x => x.ClientIp)];
        }

        await SetAsync(ip, null, "operator", now.AddMinutes(2));

        Assert.Contains(ip, all);
        Assert.AreEqual((NoiseTier.Tier1, "operator"), (added!.Tier, added.ChangedBy));
        Assert.AreEqual((NoiseTier.Tier3, "agent"), (changed!.Tier, changed.ChangedBy));
        Assert.IsNull(await GetAsync(ip));
    }

    private static async Task SetAsync(string ip, NoiseTier? tier, string changedBy, DateTimeOffset now)
    {
        await using var scope = PostgresDatabase.CreateScope();
        await Repository(scope).SetAsync(ip, tier, changedBy, now, CancellationToken.None);
    }

    private static async Task<ClientNoise?> GetAsync(string ip)
    {
        await using var scope = PostgresDatabase.CreateScope();
        return await Repository(scope).GetOrDefaultAsync(ip, CancellationToken.None);
    }

    private static IClientNoiseRepository Repository(AsyncServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<IClientNoiseRepository>();
    }
}