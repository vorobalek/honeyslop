using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class RequestRepositoryTests
{
    [TestMethod]
    public async Task AddRangeAsync_StoresTheBatchAndSummarizesTheClient()
    {
        var clientIp = $"198.51.100.{Random.Shared.Next(1, 254)}-{Unique.String(6)}";
        var now = DateTimeOffset.UtcNow;
        await using (var scope = PostgresDatabase.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IRequestRepository>().AddRangeAsync(
            [
                Record(clientIp, "a.example.test", "/.env", now.AddMinutes(-3), RequestOutcome.Generated),
                Record(clientIp, "a.example.test", "/.git/config", now.AddMinutes(-2), RequestOutcome.Hit),
                Record(clientIp, "b.example.test", "/.env", now.AddMinutes(-1), RequestOutcome.Static, "curl/8.4")
            ],
            CancellationToken.None);

        await using var readScope = PostgresDatabase.CreateScope();
        var repository = readScope.ServiceProvider.GetRequiredService<IRequestRepository>();
        var page = await repository.GetPageAsync(new RequestFilter(clientIp), 0, 10, CancellationToken.None);
        var hits = await repository.CountAsync(new RequestFilter(clientIp, Outcome: RequestOutcome.Hit), CancellationToken.None);
        var client = (await repository.GetClientsAsync(now.AddHours(-1), 0, 1000, CancellationToken.None))
            .Single(x => x.ClientIp == clientIp);

        CollectionAssert.AreEqual(new[] { "/.env", "/.git/config", "/.env" }, page.Select(x => x.Target).ToArray());
        Assert.AreEqual(1, hits);
        Assert.AreEqual(3, client.Requests);
        Assert.AreEqual(2, client.Hosts);
        Assert.AreEqual(2, client.Targets);
        Assert.AreEqual("curl/8.4", client.LastUserAgent);
    }

    [TestMethod]
    public async Task GetClientOrDefaultAsync_SummarizesOnlyThatClient()
    {
        var clientIp = $"192.0.2.{Random.Shared.Next(1, 254)}-{Unique.String(6)}";
        var now = DateTimeOffset.UtcNow;
        await using (var scope = PostgresDatabase.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IRequestRepository>().AddRangeAsync(
            [
                Record(clientIp, "a.example.test", "/.env", now.AddDays(-3), RequestOutcome.Generated),
                Record(clientIp, "a.example.test", "/.env", now.AddMinutes(-1), RequestOutcome.Hit, "curl/8.4"),
                Record(clientIp + "-other", "b.example.test", "/", now, RequestOutcome.Static)
            ],
            CancellationToken.None);

        await using var readScope = PostgresDatabase.CreateScope();
        var repository = readScope.ServiceProvider.GetRequiredService<IRequestRepository>();
        var client = await repository.GetClientOrDefaultAsync(clientIp, CancellationToken.None);
        var unknown = await repository.GetClientOrDefaultAsync(clientIp + "-unknown", CancellationToken.None);

        Assert.IsNotNull(client);
        Assert.AreEqual(2, client.Requests);
        Assert.AreEqual(1, client.Hosts);
        Assert.AreEqual(1, client.Targets);
        Assert.AreEqual(now.AddDays(-3).ToUnixTimeMilliseconds(), client.FirstSeenAt.ToUnixTimeMilliseconds());
        Assert.AreEqual(now.AddMinutes(-1).ToUnixTimeMilliseconds(), client.LastSeenAt.ToUnixTimeMilliseconds());
        Assert.AreEqual("curl/8.4", client.LastUserAgent);
        Assert.IsNull(unknown);
    }

    [TestMethod]
    public async Task DeleteReceivedBeforeAsync_RemovesOnlyOldRecords()
    {
        var clientIp = $"203.0.113.{Random.Shared.Next(1, 254)}-{Unique.String(6)}";
        var now = DateTimeOffset.UtcNow;
        await using (var scope = PostgresDatabase.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IRequestRepository>();
            await repository.AddRangeAsync(
            [
                Record(clientIp, "old.example.test", "/", now.AddYears(-20), RequestOutcome.Static),
                Record(clientIp, "new.example.test", "/", now, RequestOutcome.Static)
            ],
            CancellationToken.None);
            await repository.DeleteReceivedBeforeAsync(now.AddYears(-10), CancellationToken.None);
        }

        await using var readScope = PostgresDatabase.CreateScope();
        var left = await readScope.ServiceProvider.GetRequiredService<IRequestRepository>()
            .GetPageAsync(new RequestFilter(clientIp), 0, 10, CancellationToken.None);
        CollectionAssert.AreEqual(new[] { "new.example.test" }, left.Select(x => x.Host).ToArray());
    }

    private static RequestRecord Record(
        string clientIp,
        string host,
        string target,
        DateTimeOffset receivedAt,
        RequestOutcome outcome,
        string? userAgent = null)
    {
        return new RequestRecord
        {
            ReceivedAt = receivedAt,
            ClientIp = clientIp,
            Method = "GET",
            Host = host,
            Target = target,
            UserAgent = userAgent ?? "Mozilla/5.0",
            Headers = "User-Agent: Mozilla/5.0\n",
            Outcome = outcome,
            ResponseStatus = 200,
            DurationMs = 5,
            Instance = "test"
        };
    }
}
