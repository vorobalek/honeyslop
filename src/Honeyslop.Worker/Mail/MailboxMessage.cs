using MimeKit;

namespace Honeyslop.Worker.Mail;

public sealed record MailboxMessage(long Uid,
    MimeMessage Message);