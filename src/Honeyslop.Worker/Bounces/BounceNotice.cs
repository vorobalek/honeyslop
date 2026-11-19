using System.Text.RegularExpressions;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using MimeKit;

namespace Honeyslop.Worker.Bounces;

internal sealed partial record BounceNotice(string? MessageId, DateTimeOffset? Date, string[] ReferencedMessageIds, BounceRecipient[] Recipients)
{
    public static BounceNotice? Parse(MimeMessage message)
    {
        var walk = new Walk();
        walk.AddReferences(message);
        walk.Visit(message.Body);

        BounceRecipient[] recipients =
        [
            .. walk.Statuses.SelectMany(x => ReadRecipients(x, walk.Explanation))
        ];
        if (recipients.Length == 0)
            return null;
        return new BounceNotice(
            string.IsNullOrWhiteSpace(message.MessageId) ? null : message.MessageId,
            message.Date == DateTimeOffset.MinValue ? null : message.Date.ToUniversalTime(),
            [.. walk.References],
            recipients);
    }

    public string[] LookupIds => [.. ReferencedMessageIds.SelectMany(x => new[] { x, $"<{x}>" })];

    public AbuseReportBounce[] ToBounces(string fallbackNotificationId, IReadOnlyCollection<AbuseReport> reports, DateTimeOffset now)
    {
        AbuseReport[] referenced = [.. reports.Where(x => x.MessageId is not null && ReferencedMessageIds.Contains(x.MessageId.Trim('<', '>')))];
        if (referenced.Length == 0)
            return [];
        return
        [
            .. Recipients.Select(recipient =>
            {
                var report = referenced.FirstOrDefault(x => x.Recipients.Contains(recipient.Address, StringComparer.OrdinalIgnoreCase)) ?? referenced[0];
                return new AbuseReportBounce
                {
                    ReportId = report.Id,
                    Recipient = AttributedRecipient(report, recipient.Address),
                    Action = recipient.Action,
                    Status = recipient.Status,
                    Diagnostic = recipient.Diagnostic,
                    Mta = recipient.Mta,
                    NotificationId = MessageId ?? fallbackNotificationId,
                    BouncedAt = Date ?? now,
                    CreatedAt = now
                };
            })
        ];
    }

    private static string AttributedRecipient(AbuseReport report, string address)
    {
        if (report.Recipients.FirstOrDefault(x => string.Equals(x, address, StringComparison.OrdinalIgnoreCase)) is { } exact)
            return exact.ToLowerInvariant();
        return report.Recipients.Length == 1 ? report.Recipients[0].ToLowerInvariant() : address;
    }

    private static IEnumerable<BounceRecipient> ReadRecipients(MessageDeliveryStatus status, string? explanation)
    {
        var groups = status.StatusGroups;
        if (groups.Count < 2)
            yield break;
        var reportingMta = Typed(groups[0]["Reporting-MTA"]);
        foreach (var group in groups.Skip(1))
        {
            BounceAction? action = FirstToken(group["Action"])?.ToLowerInvariant() switch
            {
                "failed" => BounceAction.Failed,
                "delayed" => BounceAction.Delayed,
                _ => null
            };
            var address = Address(group["Original-Recipient"]) ?? Address(group["Final-Recipient"]);
            if (action is null || address is null)
                continue;
            yield return new BounceRecipient(
                address,
                action.Value,
                FirstToken(group["Status"]),
                Typed(group["Diagnostic-Code"]) ?? explanation,
                Typed(group["Remote-MTA"]) ?? reportingMta);
        }
    }

    private static string? Address(string? value)
    {
        var address = Typed(value)?.Trim('<', '>', ' ');
        return string.IsNullOrEmpty(address) || !address.Contains('@') ? null : address.ToLowerInvariant();
    }

    private static string? Typed(string? value)
    {
        var text = Collapse(value);
        if (text is null)
            return null;
        var separator = text.IndexOf(';');
        var typed = separator >= 0 ? text[(separator + 1)..].Trim() : text;
        return typed.Length == 0 ? null : typed;
    }

    private static string? FirstToken(string? value)
    {
        return Collapse(value)?.Split(' ', 2)[0];
    }

    private static string? Collapse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return Whitespace().Replace(value, " ").Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    private sealed class Walk
    {
        public List<MessageDeliveryStatus> Statuses { get; } = [];
        public HashSet<string> References { get; } = new(StringComparer.Ordinal);
        public string? Explanation { get; private set; }

        public void AddReferences(MimeMessage message)
        {
            foreach (var id in message.References)
                References.Add(id);
            if (!string.IsNullOrWhiteSpace(message.InReplyTo))
                References.Add(message.InReplyTo);
        }

        public void Visit(MimeEntity? entity)
        {
            switch (entity)
            {
                case MessageDeliveryStatus status:
                    Statuses.Add(status);
                    break;
                case MessagePart { Message: { } inner }:
                    if (!string.IsNullOrWhiteSpace(inner.MessageId))
                        References.Add(inner.MessageId);
                    AddReferences(inner);
                    Visit(inner.Body);
                    break;
                case Multipart multipart:
                    if (multipart.ContentType.IsMimeType("multipart", "report") && multipart.FirstOrDefault() is TextPart { IsPlain: true } text)
                        Explanation ??= Collapse(text.Text);
                    foreach (var part in multipart)
                        Visit(part);
                    break;
            }
        }
    }
}