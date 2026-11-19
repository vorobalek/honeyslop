namespace Honeyslop.Worker.Mail;

public sealed class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? From { get; set; }
    public bool StartTls { get; set; } = true;

    public bool Configured =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password) &&
        !string.IsNullOrWhiteSpace(From);
}