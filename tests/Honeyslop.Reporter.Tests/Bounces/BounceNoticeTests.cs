using System.Text;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Reporter.Bounces;
using MimeKit;

namespace Honeyslop.Reporter.Tests.Bounces;

[TestClass]
public sealed class BounceNoticeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 18, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Parse_ForwardedEximBounce_ReadsTheFailureAndTheReturnedMessageId()
    {
        var notice = BounceNotice.Parse(Load(Notifications.ForwardedExim))!;

        Assert.AreEqual("E1xU5RK-0000000An03-1jHL@s7.hoster.example", notice.MessageId);
        Assert.AreEqual(new DateTimeOffset(2026, 11, 19, 16, 56, 50, TimeSpan.Zero), notice.Date);
        Assert.AreEqual(TimeSpan.Zero, notice.Date!.Value.Offset);
        CollectionAssert.Contains(notice.ReferencedMessageIds, Notifications.ReportMessageId);
        var recipient = notice.Recipients.Single();
        Assert.AreEqual("abuse@hoster.example", recipient.Address);
        Assert.AreEqual(BounceAction.Failed, recipient.Action);
        Assert.AreEqual("5.0.0", recipient.Status);
        Assert.AreEqual("s7.hoster.example", recipient.Mta);
        Assert.Contains("retry timeout exceeded", recipient.Diagnostic!);
        Assert.DoesNotContain("\n", recipient.Diagnostic!);
    }

    [TestMethod]
    public void Parse_PostfixReport_TakesFailedAndDelayedRecipientsWithTheirDiagnostics()
    {
        var notice = BounceNotice.Parse(Load(Notifications.Postfix))!;

        CollectionAssert.Contains(notice.ReferencedMessageIds, "report-23.20261119165900.aaaaaa@example.test");
        Assert.HasCount(2, notice.Recipients);
        var failed = notice.Recipients[0];
        Assert.AreEqual("noc@hoster.example", failed.Address);
        Assert.AreEqual(BounceAction.Failed, failed.Action);
        Assert.AreEqual("5.1.1", failed.Status);
        Assert.AreEqual("inbound.internal.hoster.example", failed.Mta);
        Assert.AreEqual("550 5.1.1 <desk@internal.hoster.example>: Recipient address rejected: User unknown", failed.Diagnostic);
        var delayed = notice.Recipients[1];
        Assert.AreEqual("abuse@hoster.example", delayed.Address);
        Assert.AreEqual(BounceAction.Delayed, delayed.Action);
        Assert.AreEqual("mx.hoster.example", delayed.Mta);
        Assert.AreEqual("connect to inbound.hoster.example: Connection timed out", delayed.Diagnostic);
    }

    [TestMethod]
    public void Parse_SuccessReportOrPlainMessage_IsNotABounce()
    {
        Assert.IsNull(BounceNotice.Parse(Load(Notifications.Success)));
        Assert.IsNull(BounceNotice.Parse(Load(Notifications.Plain)));
    }

    [TestMethod]
    public void ToBounces_AttributesTheBounceToTheReferencedReportAndItsRecipient()
    {
        var notice = BounceNotice.Parse(Load(Notifications.Postfix))!;
        var report = Report(23, "report-23.20261119165900.aaaaaa@example.test", "NOC@hoster.example", "abuse@hoster.example");

        var bounces = notice.ToBounces("INBOX/7/42", [report, Report(24, "other@example.test", "abuse@hoster.example")], Now);

        Assert.HasCount(2, bounces);
        Assert.IsTrue(bounces.All(x => x.ReportId == 23));
        CollectionAssert.AreEqual(new[] { "noc@hoster.example", "abuse@hoster.example" }, bounces.Select(x => x.Recipient).ToArray());
        Assert.IsTrue(bounces.All(x => x.NotificationId == "20261119170000.ABC123@mx.hoster.example"));
        Assert.IsTrue(bounces.All(x => x.BouncedAt == new DateTimeOffset(2026, 11, 19, 17, 0, 0, TimeSpan.Zero) && x.CreatedAt == Now));
    }

    [TestMethod]
    public void ToBounces_SingleRecipientReport_GetsTheBounceOfAForwardedAddress()
    {
        var notice = BounceNotice.Parse(Load(Notifications.ForwardedExim.Replace("Final-Recipient: rfc822;abuse@hoster.example", "Final-Recipient: rfc822;desk@internal.hoster.example")))!;

        var bounce = notice.ToBounces("INBOX/7/43", [Report(22, Notifications.ReportMessageId, "abuse@hoster.example")], Now).Single();

        Assert.AreEqual("abuse@hoster.example", bounce.Recipient);
    }

    [TestMethod]
    public void ToBounces_MessageIdStoredInAngleBrackets_StillMatches()
    {
        var notice = BounceNotice.Parse(Load(Notifications.ForwardedExim))!;

        var bounce = notice.ToBounces("INBOX/7/45", [Report(22, $"<{Notifications.ReportMessageId}>", "abuse@hoster.example")], Now).Single();

        Assert.AreEqual(22, bounce.ReportId);
        CollectionAssert.Contains(notice.LookupIds, $"<{Notifications.ReportMessageId}>");
    }

    [TestMethod]
    public void ToBounces_UnknownReport_IsIgnored()
    {
        var notice = BounceNotice.Parse(Load(Notifications.ForwardedExim))!;

        Assert.IsEmpty(notice.ToBounces("INBOX/7/44", [Report(99, "someone-else@example.test", "abuse@hoster.example")], Now));
    }

    private static AbuseReport Report(long id, string messageId, params string[] recipients)
    {
        return new AbuseReport
        {
            Id = id,
            ClientIp = "203.0.113.7",
            Recipients = recipients,
            Subject = "HTTP scanning",
            Body = "Hello",
            RequestedBy = "test",
            Status = AbuseReportStatus.Sent,
            MessageId = messageId
        };
    }

    private static MimeMessage Load(string text)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        return MimeMessage.Load(stream);
    }
}