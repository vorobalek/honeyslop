using MimeKit;

namespace Honeyslop.Reporter.Mail;

public sealed record MailboxMessage(long Uid, MimeMessage Message);