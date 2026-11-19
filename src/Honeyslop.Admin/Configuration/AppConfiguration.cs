using Honeyslop.Common;
using Honeyslop.Data.Requests;

namespace Honeyslop.Admin.Configuration;

public static class AppConfiguration
{
    public static readonly int Port = "PORT"
        .GetEnvironmentValueWithFallback(8080);

    public static readonly string DbConnectionString = "DB_CONNECTION_STRING"
        .RequiredEnvironmentValue;

    public static readonly int MaintenanceIntervalSeconds = "MAINTENANCE_INTERVAL_SECONDS"
        .GetEnvironmentValueWithFallback(60);

    public static readonly string? LlmUrl = "LLM_URL"
        .EnvironmentValue;

    public static readonly string? OidcAuthority = "OIDC_AUTHORITY"
        .EnvironmentValue;

    public static readonly string OidcClientId = "OIDC_CLIENT_ID"
        .GetEnvironmentValueWithFallback("honeyslop");

    public static readonly string? OidcClientSecret = "OIDC_CLIENT_SECRET"
        .EnvironmentValue;

    public static readonly string UserAgent = "USER_AGENT"
        .GetEnvironmentValueWithFallback(Constants.UserAgent);

    public static readonly string? SmtpFrom = "SMTP_FROM"
        .EnvironmentValue;

    public static readonly int MaxTargetLength = "MAX_TARGET_LENGTH"
        .GetEnvironmentValueWithFallback(RequestRules.DefaultMaxTargetLength);

    public static readonly string? StaticExtensions = "STATIC_EXTENSIONS"
        .EnvironmentValue;

    public static readonly int ResponseDelayMilliseconds = "RESPONSE_DELAY_MILLISECONDS"
        .GetEnvironmentValueWithFallback(0);

    public static readonly string? AbuseReportSenderName = "ABUSE_REPORT_SENDER_NAME"
        .EnvironmentValue;

    public static readonly string GeoIpDatabase = "GEOIP_DATABASE"
        .GetEnvironmentValueWithFallback("/app/geoip/country.mmdb");

    public static readonly string? McpResource = "MCP_RESOURCE"
        .EnvironmentValue;

    public static bool SignInEnabled => !string.IsNullOrWhiteSpace(OidcAuthority);

    public static bool McpEnabled => SignInEnabled && !string.IsNullOrWhiteSpace(McpResource);
}