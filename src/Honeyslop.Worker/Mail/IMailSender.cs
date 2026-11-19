using MimeKit;

namespace Honeyslop.Worker.Mail;

public interface IMailSender
{
    Task SendAsync(MimeMessage message, CancellationToken cancellationToken);
}