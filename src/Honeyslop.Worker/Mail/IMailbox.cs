namespace Honeyslop.Worker.Mail;

public interface IMailbox
{
    Task<MailboxBatch> ReadAsync(string folder, long? uidValidity, long afterUid, int limit, CancellationToken cancellationToken);
}