namespace Honeyslop.Worker.Mail;

public sealed record MailboxBatch(long UidValidity, long LastUid, MailboxMessage[] Messages, bool HasMore);