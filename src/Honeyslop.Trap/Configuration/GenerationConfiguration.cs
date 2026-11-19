using Honeyslop.Common;
using Honeyslop.Data.Requests;

namespace Honeyslop.Trap.Configuration;

public static class GenerationConfiguration
{
    public static readonly int Workers = "GENERATION_WORKERS"
        .GetEnvironmentValueWithFallback(6);

    public static readonly int MaxWaiters = "GENERATION_MAX_WAITERS"
        .GetEnvironmentValueWithFallback(500);

    public static readonly int MaxAttempts = "GENERATION_MAX_ATTEMPTS"
        .GetEnvironmentValueWithFallback(3);

    public static readonly int MaxTargetLength = "MAX_TARGET_LENGTH"
        .GetEnvironmentValueWithFallback(RequestRules.DefaultMaxTargetLength);

    public static readonly int MaxRequestBodyBytes = "MAX_REQUEST_BODY_BYTES"
        .GetEnvironmentValueWithFallback(2048);

    public static readonly int MemoryCacheSeconds = "MEMORY_CACHE_SECONDS"
        .GetEnvironmentValueWithFallback(60);

    public static readonly int MemoryCacheMegabytes = "MEMORY_CACHE_MEGABYTES"
        .GetEnvironmentValueWithFallback(64);

    public static readonly int HitFlushSeconds = "HIT_FLUSH_SECONDS"
        .GetEnvironmentValueWithFallback(15);

    public static readonly string? StaticExtensions = "STATIC_EXTENSIONS"
        .EnvironmentValue;

    public static readonly int ResponseDelayMilliseconds = "RESPONSE_DELAY_MILLISECONDS"
        .GetEnvironmentValueWithFallback(0);
}