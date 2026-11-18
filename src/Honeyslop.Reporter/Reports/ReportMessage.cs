using System.Globalization;
using System.Security.Cryptography;
using Honeyslop.Data.Entities;
using Honeyslop.Reports;
using MimeKit;

namespace Honeyslop.Reporter.Reports;

internal static class ReportMessage
{
    public static MimeMessage Create(AbuseReport report, MailboxAddress sender, byte[] archive, DateTimeOffset now)
    {
        var message = new MimeMessage
        {
            Subject = report.Subject,
            Date = now,
            MessageId = MessageIdOf(report, sender, now)
        };
        message.From.Add(sender);
        message.To.AddRange(report.Recipients.Select(MailboxAddress.Parse));

        var body = new BodyBuilder { TextBody = report.Body };
        body.Attachments.Add(RequestLogArchive.FileName(report.ClientIp), archive, new ContentType("application", "zip"));
        message.Body = body.ToMessageBody();
        return message;
    }

    public static string MessageIdOf(AbuseReport report, MailboxAddress sender, DateTimeOffset now)
    {
        var stamp = now.UtcDateTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        return $"report-{report.Id}.{stamp}.{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(3))}@{sender.Domain}";
    }
}
