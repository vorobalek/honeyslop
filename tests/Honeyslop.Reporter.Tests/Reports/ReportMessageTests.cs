using System.IO.Compression;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Reporter.Reports;
using Honeyslop.Reports;
using MimeKit;

namespace Honeyslop.Reporter.Tests.Reports;

[TestClass]
public sealed class ReportMessageTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Create_AddressesTheAbuseContactsAndAttachesTheZippedLog()
    {
        var report = new AbuseReport
        {
            Id = 42,
            ClientIp = "2001:db8::7",
            Recipients = ["abuse@example.test", "noc@example.test"],
            Subject = "HTTP scanning from 2001:db8::7",
            Body = "Hello",
            RequestedBy = "test",
            Status = AbuseReportStatus.Pending
        };
        using var archive = new RequestArchive(report.ClientIp, 1, 1);
        archive.Add(Request(1, Now, "GET", "/.env", "Host: a.example.test\n", null));

        var message = ReportMessage.Create(report, new MailboxAddress("Example Sender, security team", "security@example.test"), archive.ToArray(), Now);

        Assert.AreEqual("security@example.test", message.From.Mailboxes.Single().Address);
        Assert.AreEqual("Example Sender, security team", message.From.Mailboxes.Single().Name);
        Assert.Contains("\"Example Sender, security team\" <security@example.test>", message.Headers[HeaderId.From]!);
        CollectionAssert.AreEqual(new[] { "abuse@example.test", "noc@example.test" }, message.To.Mailboxes.Select(x => x.Address).ToArray());
        Assert.AreEqual(report.Subject, message.Subject);
        Assert.MatchesRegex(@"^report-42\.20261118120000\.[0-9a-f]{6}@example\.test$", message.MessageId);
        Assert.AreEqual("Hello", message.TextBody);
        var attachment = message.Attachments.OfType<MimePart>().Single();
        Assert.AreEqual("2001-db8--7-requests.zip", attachment.FileName);
        using var content = new MemoryStream();
        attachment.Content!.DecodeTo(content);
        Assert.Contains("All 1 requests we kept from 2001:db8::7", Unzip(content.ToArray()));
        Assert.Contains("GET /.env", Unzip(content.ToArray()));
    }

    private static string Unzip(byte[] archive)
    {
        using var zip = new ZipArchive(new MemoryStream(archive), ZipArchiveMode.Read);
        using var reader = new StreamReader(zip.GetEntry(RequestArchive.FileName)!.Open());
        return reader.ReadToEnd();
    }

    private static RequestRecord Request(long id, DateTimeOffset receivedAt, string method, string target, string headers, string? body)
    {
        return new RequestRecord
        {
            Id = id,
            ReceivedAt = receivedAt,
            ClientIp = "203.0.113.9",
            Method = method,
            Host = "a.example.test",
            Target = target,
            Headers = headers,
            Body = body,
            Outcome = RequestOutcome.Hit,
            Instance = "test"
        };
    }
}
