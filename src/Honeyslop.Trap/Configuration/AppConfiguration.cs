using Honeyslop.Common;

namespace Honeyslop.Trap.Configuration;

public static class AppConfiguration
{
    public static readonly int TrapPort = "TRAP_PORT"
        .GetEnvironmentValueWithFallback(8080);

    public static readonly int ManagementPort = "MANAGEMENT_PORT"
        .GetEnvironmentValueWithFallback(8081);

    public static readonly string DbConnectionString = "DB_CONNECTION_STRING"
        .RequiredEnvironmentValue;

    public static readonly string InstanceName = "POD_NAME"
        .GetEnvironmentValueWithFallback(Environment.MachineName);

    public static readonly string TrustedNetworks = "TRUSTED_NETWORKS"
        .GetEnvironmentValueWithFallback("10.0.0.0/8");
}