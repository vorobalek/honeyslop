using MimeKit;

namespace Honeyslop.Reporter.Mail;

public interface IMailSender
{
    Task SendAsync(MimeMessage message, CancellationToken cancellationToken);
}