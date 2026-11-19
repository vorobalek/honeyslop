using Honeyslop.Common;

namespace Honeyslop.Reporter.Configuration;

public static class AppConfiguration
{
    public static readonly int ManagementPort = "MANAGEMENT_PORT"
        .GetEnvironmentValueWithFallback(8081);

    public static readonly string DbConnectionString = "DB_CONNECTION_STRING"
        .RequiredEnvironmentValue;

    public static readonly string InstanceName = "POD_NAME"
        .GetEnvironmentValueWithFallback(Environment.MachineName);

    public static readonly string? SmtpHost = "SMTP_HOST"
        .EnvironmentValue;

    public static readonly int SmtpPort = "SMTP_PORT"
        .GetEnvironmentValueWithFallback(587);

    public static readonly string? SmtpUsername = "SMTP_USERNAME"
        .EnvironmentValue;

    public static readonly string? SmtpPassword = "SMTP_PASSWORD"
        .EnvironmentValue;

    public static readonly string? SmtpFrom = "SMTP_FROM"
        .EnvironmentValue;

    public static readonly bool SmtpStartTls = "SMTP_STARTTLS"
        .GetEnvironmentValueWithFallback(true);

    public static readonly int MaxAttachedRequests = "MAX_ATTACHED_REQUESTS"
        .GetEnvironmentValueWithFallback(50000);

    public static readonly string? ImapHost = "IMAP_HOST"
        .EnvironmentValue;

    public static readonly int ImapPort = "IMAP_PORT"
        .GetEnvironmentValueWithFallback(993);

    public static readonly string? ImapUsername = "IMAP_USERNAME"
        .EnvironmentValue;

    public static readonly string? ImapPassword = "IMAP_PASSWORD"
        .EnvironmentValue;

    public static readonly string[] ImapFolders =
    [
        .. "IMAP_FOLDERS"
            .GetEnvironmentValueWithFallback("INBOX")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
    ];

    public static readonly int ImapPollSeconds = "IMAP_POLL_SECONDS"
        .GetEnvironmentValueWithFallback(300);
}