using Honeyslop.Common;

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
}
