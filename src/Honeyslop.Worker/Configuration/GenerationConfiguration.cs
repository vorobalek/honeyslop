using Honeyslop.Common;

namespace Honeyslop.Worker.Configuration;

public static class GenerationConfiguration
{
    public static readonly int Workers = "GENERATION_WORKERS"
        .GetEnvironmentValueWithFallback(6);

    public static readonly int MaxAttempts = "GENERATION_MAX_ATTEMPTS"
        .GetEnvironmentValueWithFallback(3);
}