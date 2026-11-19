using Honeyslop.Data.Entities;
using Honeyslop.Data.Repositories;
using Honeyslop.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class ClientProbeRepositoryTests
{
    [TestMethod]
    public async Task TryReplaceAsync_OnlyTheWriterThatSawTheCurrentVersionWins()
    {
        var ip = $"198.51.100.{Random.Shared.Next(1, 254)}-{Unique.String(6)}";
        var now = DateTimeOffset.UtcNow;
        await using var scope = PostgresDatabase.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IClientProbeRepository>();

        var added = await repository.TryAddAsync(Probe(ip, 15_000, null, now), CancellationToken.None);
        var addedAgain = await repository.TryAddAsync(Probe(ip, 1, null, now), CancellationToken.None);
        var replacement = Probe(ip, 9_900, 10_000, now);
        replacement.LastRequestId = 42;
        var first = await repository.TryReplaceAsync(replacement, 0, CancellationToken.None);
        var stale = await repository.TryReplaceAsync(Probe(ip, 20_000, null, now), 0, CancellationToken.None);
        var stored = await repository.GetOrDefaultAsync(ip, CancellationToken.None);
        var listed = await repository.GetAnyOfAsync([ip, "192.0.2.250"], CancellationToken.None);

        Assert.IsTrue(added);
        Assert.IsFalse(addedAgain);
        Assert.IsTrue(first);
        Assert.IsFalse(stale);
        Assert.AreEqual((9_900, (int?)10_000, 1, 42L), (stored!.DelayMilliseconds, stored.TimeoutMilliseconds, stored.Version, stored.LastRequestId));
        Assert.AreEqual(ip, listed.Single().ClientIp);
    }

    private static ClientProbe Probe(string ip, int delay, int? timeout, DateTimeOffset now)
    {
        return new ClientProbe
        {
            ClientIp = ip,
            DelayMilliseconds = delay,
            TimeoutMilliseconds = timeout,
            ProbeEvery = 20,
            UpdatedAt = now
        };
    }
}