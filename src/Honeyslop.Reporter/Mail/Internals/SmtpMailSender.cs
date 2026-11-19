using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Honeyslop.Reporter.Mail.Internals;

internal sealed class SmtpMailSender(IOptions<SmtpOptions> options) : IMailSender
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(1);

    public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        var smtp = options.Value;
        using var client = new SmtpClient();
        client.Timeout = (int)Timeout.TotalMilliseconds;
        await client.ConnectAsync(
            smtp.Host!,
            smtp.Port,
            smtp.StartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
            cancellationToken);
        await client.AuthenticateAsync(smtp.Username!, smtp.Password!, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
