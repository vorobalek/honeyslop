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
}