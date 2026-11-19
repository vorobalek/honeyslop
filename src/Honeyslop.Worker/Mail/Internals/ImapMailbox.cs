using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Extensions.Options;

namespace Honeyslop.Worker.Mail.Internals;

internal sealed class ImapMailbox(IOptions<ImapOptions> options) : IMailbox
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(1);

    public async Task<MailboxBatch> ReadAsync(string folder, long? uidValidity, long afterUid, int limit, CancellationToken cancellationToken)
    {
        var imap = options.Value;
        using var client = new ImapClient();
        client.Timeout = (int)Timeout.TotalMilliseconds;
        await client.ConnectAsync(imap.Host!, imap.Port, SecureSocketOptions.Auto, cancellationToken);
        await client.AuthenticateAsync(imap.Username!, imap.Password!, cancellationToken);
        var mailFolder = await client.GetFolderAsync(folder, cancellationToken);
        await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        var validity = (long)mailFolder.UidValidity;
        var after = validity == uidValidity ? afterUid : 0;
        var summaries = await mailFolder.FetchAsync(
            new UniqueIdRange(new UniqueId((uint)after + 1), UniqueId.MaxValue),
            MessageSummaryItems.UniqueId | MessageSummaryItems.BodyStructure,
            cancellationToken);
        IMessageSummary[] fresh = [.. summaries.Where(x => x.UniqueId.Id > after).OrderBy(x => x.UniqueId.Id)];
        IMessageSummary[] batch = [.. fresh.Take(limit)];

        var messages = new List<MailboxMessage>();
        foreach (var summary in batch.Where(x => IsDeliveryReport(x.Body)))
            messages.Add(new MailboxMessage(summary.UniqueId.Id, await mailFolder.GetMessageAsync(summary.UniqueId, cancellationToken)));

        await client.DisconnectAsync(true, cancellationToken);
        return new MailboxBatch(validity, batch.Length > 0 ? batch[^1].UniqueId.Id : after, [.. messages], fresh.Length > batch.Length);
    }

    private static bool IsDeliveryReport(BodyPart? part)
    {
        return part switch
        {
            BodyPartMultipart multipart => multipart.BodyParts.Any(IsDeliveryReport),
            BodyPartMessage message => IsDeliveryReport(message.Body),
            BodyPartBasic basic => basic.ContentType.IsMimeType("message", "delivery-status") ||
                                   basic.ContentType.IsMimeType("message", "global-delivery-status"),
            _ => false
        };
    }
}