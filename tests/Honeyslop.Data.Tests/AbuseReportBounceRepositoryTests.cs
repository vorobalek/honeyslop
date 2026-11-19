using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class AbuseReportBounceRepositoryTests
{
    [TestMethod]
    public async Task AddAsync_RecordsEachNotificationAndRecipientOnce()
    {
        var recipient = $"abuse@{Unique.String(8)}.example";
        var notification = $"{Unique.String(12)}@mx.example";
        var now = DateTimeOffset.UtcNow;

        var added = new[]
        {
            await AddAsync([Bounce(1, recipient, notification, now, "5.0.0", "retry timeout exceeded")]),
            await AddAsync(
            [
                Bounce(1, recipient, notification, now, "5.0.0", "retry timeout exceeded"),
                Bounce(1, $"other-{recipient}", notification, now, "5.1.1", "no such user")
            ])
        };

        CollectionAssert.AreEqual(new[] { 1, 1 }, added);
        await using var scope = PostgresDatabase.CreateScope();
        var stored = await Repository(scope).GetContactsAsync([recipient, $"other-{recipient}"], CancellationToken.None);
        Assert.HasCount(2, stored);
        Assert.IsTrue(stored.All(x => x.Bounces == 1));
    }

    [TestMethod]
    public async Task GetContactsAsync_CountsBouncesAndReportsAndTakesTheLatestAnswer()
    {
        var recipient = $"abuse@{Unique.String(8)}.example";
        var now = DateTimeOffset.UtcNow;
        await AddAsync(
        [
            Bounce(10, recipient, Unique.String(12), now.AddDays(-2), "4.2.2", "mailbox full"),
            Bounce(11, recipient, Unique.String(12), now.AddDays(-1), "5.0.0", "retry timeout exceeded"),
            Bounce(11, recipient, Unique.String(12), now.AddDays(-3), "4.4.1", "connection timed out")
        ]);

        await using var scope = PostgresDatabase.CreateScope();
        var contact = (await Repository(scope).GetContactsAsync([recipient], CancellationToken.None)).Single();

        Assert.AreEqual(recipient, contact.Recipient);
        Assert.AreEqual(3, contact.Bounces);
        Assert.AreEqual(2, contact.Reports);
        Assert.AreEqual(11, contact.LastReportId);
        Assert.AreEqual("5.0.0", contact.LastStatus);
        Assert.AreEqual("retry timeout exceeded", contact.LastDiagnostic);
        Assert.AreEqual(now.AddDays(-1).ToUnixTimeMilliseconds(), contact.LastBouncedAt.ToUnixTimeMilliseconds(), 1);
    }

    [TestMethod]
    public async Task GetContactsPageAsync_FiltersByPeriodAndSearch()
    {
        var host = $"{Unique.String(8)}.example";
        var now = DateTimeOffset.UtcNow;
        await AddAsync(
        [
            Bounce(20, $"abuse@{host}", Unique.String(12), now.AddDays(-1), "5.0.0", "retry timeout exceeded"),
            Bounce(21, $"noc@{host}", Unique.String(12), now.AddDays(-30), "5.1.1", "no such user"),
            Bounce(22, $"abuse@other-{Unique.String(8)}.example", Unique.String(12), now.AddDays(-1), "5.7.1", $"rejected by {host} policy")
        ]);

        await using var scope = PostgresDatabase.CreateScope();
        var repository = Repository(scope);
        var recent = await repository.GetContactsPageAsync(now.AddDays(-7), host, 0, 50, CancellationToken.None);
        var all = await repository.CountContactsAsync(DateTimeOffset.MinValue, host, CancellationToken.None);

        string[] recipients = [.. recent.Select(x => x.Recipient)];
        Assert.HasCount(2, recipients);
        Assert.Contains($"abuse@{host}", recipients);
        Assert.DoesNotContain($"noc@{host}", recipients);
        Assert.AreEqual(3, all);
    }

    [TestMethod]
    public async Task GetByReportAsync_ReturnsTheBouncesOfThatReportInOrder()
    {
        var reportId = Random.Shared.NextInt64(1_000_000, long.MaxValue);
        var now = DateTimeOffset.UtcNow;
        await AddAsync(
        [
            Bounce(reportId, "abuse@late.example", Unique.String(12), now, "5.0.0", null),
            Bounce(reportId, "abuse@early.example", Unique.String(12), now.AddHours(-1), "4.4.1", null),
            Bounce(reportId + 1, "abuse@other.example", Unique.String(12), now, "5.0.0", null)
        ]);

        await using var scope = PostgresDatabase.CreateScope();
        var bounces = await Repository(scope).GetByReportAsync(reportId, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "abuse@early.example", "abuse@late.example" }, bounces.Select(x => x.Recipient).ToArray());
    }

    private static AbuseReportBounce Bounce(long reportId, string recipient, string notificationId, DateTimeOffset bouncedAt, string status, string? diagnostic)
    {
        return new AbuseReportBounce
        {
            ReportId = reportId,
            Recipient = recipient,
            Action = status.StartsWith('4') ? BounceAction.Delayed : BounceAction.Failed,
            Status = status,
            Diagnostic = diagnostic,
            Mta = "mx.example",
            NotificationId = notificationId,
            BouncedAt = bouncedAt,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static async Task<int> AddAsync(AbuseReportBounce[] bounces)
    {
        await using var scope = PostgresDatabase.CreateScope();
        return await Repository(scope).AddAsync(bounces, CancellationToken.None);
    }

    private static IAbuseReportBounceRepository Repository(AsyncServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<IAbuseReportBounceRepository>();
    }
}