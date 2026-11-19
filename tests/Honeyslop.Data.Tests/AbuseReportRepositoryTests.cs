using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public sealed class AbuseReportRepositoryTests
{
    private const int MaxAttempts = 3;

    [TestMethod]
    public async Task TryAddAsync_SecondReportWhileOneIsPending_IsRejected()
    {
        var clientIp = UniqueIp();
        var now = DateTimeOffset.UtcNow;

        var first = await AddAsync(Report(clientIp, now));
        var second = await AddAsync(Report(clientIp, now));

        Assert.IsTrue(first);
        Assert.IsFalse(second);
    }

    [TestMethod]
    public async Task TryClaimNextAsync_AliveClaimIsKept_StaleClaimIsTakenOver()
    {
        var now = DateTimeOffset.UtcNow;
        await DrainAsync(now);
        var clientIp = UniqueIp();
        await AddAsync(Report(clientIp, now));
        await using var scope = PostgresDatabase.CreateScope();
        var repository = scope.Reports();

        var first = await repository.TryClaimNextAsync("reporter-a", now, now.AddMinutes(-2), MaxAttempts, CancellationToken.None);
        var whileAlive = await repository.TryClaimNextAsync("reporter-b", now.AddSeconds(30), now.AddMinutes(-2), MaxAttempts, CancellationToken.None);
        var later = now.AddMinutes(3);
        var takenOver = await repository.TryClaimNextAsync("reporter-b", later, later.AddMinutes(-2), MaxAttempts, CancellationToken.None);
        var lateCompletion = await repository.CompleteAsync(first!.Id, "reporter-a", "<late@test>", "late <a@test>", 1, 10, later, CancellationToken.None);
        var completion = await repository.CompleteAsync(first.Id, "reporter-b", "<sent@test>", "\"Example Sender\" <security@example.test>", 2, 20, later, CancellationToken.None);
        var stored = (await repository.GetByClientAsync(clientIp, CancellationToken.None)).Single();

        Assert.AreEqual(clientIp, first.ClientIp);
        Assert.AreEqual(1, first.Attempts);
        Assert.IsNull(whileAlive);
        Assert.AreEqual(first.Id, takenOver!.Id);
        Assert.AreEqual(2, takenOver.Attempts);
        Assert.IsFalse(lateCompletion);
        Assert.IsTrue(completion);
        Assert.AreEqual(AbuseReportStatus.Sent, stored.Status);
        Assert.AreEqual("<sent@test>", stored.MessageId);
        Assert.AreEqual(2, stored.AttachedRequests);
        Assert.AreEqual(20, stored.LastRequestId);
        Assert.AreEqual("\"Example Sender\" <security@example.test>", stored.SentFrom);
    }

    [TestMethod]
    public async Task RetryAsync_WaitsUntilTheRetryTime()
    {
        var now = DateTimeOffset.UtcNow;
        await DrainAsync(now);
        await AddAsync(Report(UniqueIp(), now));
        await using var scope = PostgresDatabase.CreateScope();
        var repository = scope.Reports();

        var claimed = await repository.TryClaimNextAsync("reporter", now, now.AddMinutes(-2), MaxAttempts, CancellationToken.None);
        await repository.RetryAsync(claimed!.Id, "reporter", "421 try later", now.AddMinutes(1), now, CancellationToken.None);
        var tooEarly = await repository.TryClaimNextAsync("reporter", now.AddSeconds(30), now.AddMinutes(-2), MaxAttempts, CancellationToken.None);
        var onTime = await repository.TryClaimNextAsync("reporter", now.AddMinutes(1), now, MaxAttempts, CancellationToken.None);

        Assert.IsNull(tooEarly);
        Assert.AreEqual(claimed.Id, onTime!.Id);
        Assert.AreEqual("421 try later", onTime.Error);
    }

    [TestMethod]
    public async Task FailExhaustedAsync_GivesUpOnReportsWhoseReporterKeptDying()
    {
        var now = DateTimeOffset.UtcNow;
        await DrainAsync(now);
        var clientIp = UniqueIp();
        await AddAsync(Report(clientIp, now));
        await using var scope = PostgresDatabase.CreateScope();
        var repository = scope.Reports();

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var at = now.AddMinutes(attempt * 5);
            Assert.IsNotNull(await repository.TryClaimNextAsync($"reporter-{attempt}", at, at.AddMinutes(-2), MaxAttempts, CancellationToken.None));
        }

        var end = now.AddMinutes(MaxAttempts * 5);
        var exhausted = await repository.TryClaimNextAsync("reporter", end, end.AddMinutes(-2), MaxAttempts, CancellationToken.None);
        var failed = await repository.FailExhaustedAsync(MaxAttempts, end.AddMinutes(-2), end, CancellationToken.None);
        var stored = (await repository.GetByClientAsync(clientIp, CancellationToken.None)).Single();

        Assert.IsNull(exhausted);
        Assert.AreEqual(1, failed);
        Assert.AreEqual(AbuseReportStatus.Failed, stored.Status);
        Assert.Contains("Gave up after 3 attempts", stored.Error!);
    }

    [TestMethod]
    public async Task CountByStateAsync_TellsQueuedSendingRetryingSentAndFailedApart()
    {
        var now = DateTimeOffset.UtcNow;
        await DrainAsync(now);
        await using var scope = PostgresDatabase.CreateScope();
        var repository = scope.Reports();
        var before = await repository.CountByStateAsync(CancellationToken.None);

        for (var index = 0; index < 4; index++)
            await AddAsync(Report(UniqueIp(), now));
        var sending = await repository.TryClaimNextAsync("reporter", now, now.AddMinutes(-2), MaxAttempts, CancellationToken.None);
        var retrying = await repository.TryClaimNextAsync("reporter", now, now.AddMinutes(-2), MaxAttempts, CancellationToken.None);
        var sent = await repository.TryClaimNextAsync("reporter", now, now.AddMinutes(-2), MaxAttempts, CancellationToken.None);
        await repository.RetryAsync(retrying!.Id, "reporter", "421 try later", now.AddMinutes(1), now, CancellationToken.None);
        await repository.CompleteAsync(sent!.Id, "reporter", "<sent@test>", "security@example.test", 1, 1, now, CancellationToken.None);
        await AddAsync(Report(UniqueIp(), now));
        var failed = await repository.TryClaimNextAsync("reporter", now, now.AddMinutes(-2), MaxAttempts, CancellationToken.None);
        await repository.FailAsync(failed!.Id, "reporter", "550 rejected", now, CancellationToken.None);
        var after = await repository.CountByStateAsync(CancellationToken.None);

        Assert.IsNotNull(sending);
        foreach (var state in Enum.GetValues<AbuseReportState>())
            Assert.AreEqual(1, after[state] - before[state], state.ToString());
    }

    [TestMethod]
    public async Task GetByMessageIdsAsync_FindsSentReportsByTheirMessageId()
    {
        var now = DateTimeOffset.UtcNow;
        var messageId = $"report-{Unique.String(8)}@example.test";
        var report = Report(UniqueIp(), now);
        report.Status = AbuseReportStatus.Sent;
        report.MessageId = messageId;
        await AddAsync(report);
        await AddAsync(Report(UniqueIp(), now));

        await using var scope = PostgresDatabase.CreateScope();
        var found = await scope.Reports().GetByMessageIdsAsync([messageId, "unknown@example.test"], CancellationToken.None);

        Assert.AreEqual(report.Id, found.Single().Id);
    }

    private static async Task DrainAsync(DateTimeOffset now)
    {
        var future = now.AddYears(100);
        await using var scope = PostgresDatabase.CreateScope();
        var repository = scope.Reports();
        while (await repository.TryClaimNextAsync("drain", future, future, int.MaxValue, CancellationToken.None) is { } report)
            await repository.FailAsync(report.Id, "drain", "drained by a test", future, CancellationToken.None);
    }

    private static async Task<bool> AddAsync(AbuseReport report)
    {
        await using var scope = PostgresDatabase.CreateScope();
        return await scope.Reports().TryAddAsync(report, CancellationToken.None);
    }

    private static AbuseReport Report(string clientIp, DateTimeOffset now)
    {
        return new AbuseReport
        {
            ClientIp = clientIp,
            Recipients = ["abuse@example.test"],
            Subject = $"HTTP scanning from {clientIp}",
            Body = "Hello",
            RequestedBy = "test",
            Status = AbuseReportStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static string UniqueIp()
    {
        return $"192.0.2.{Random.Shared.Next(1, 254)}-{Unique.String(6)}";
    }
}

internal static class AbuseReportScopeExtensions
{
    public static IAbuseReportRepository Reports(this AsyncServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<IAbuseReportRepository>();
    }
}