using Honeyslop.Common;

namespace Honeyslop.Worker.Configuration;

public static class LlmConfiguration
{
    public static readonly string Url = "LLM_URL"
        .RequiredEnvironmentValue;

    public static readonly string Model = "LLM_MODEL"
        .GetEnvironmentValueWithFallback("honeyslop");

    public static readonly int MaxTokens = "LLM_MAX_TOKENS"
        .GetEnvironmentValueWithFallback(1024);

    public static readonly double Temperature = "LLM_TEMPERATURE"
        .GetEnvironmentValueWithFallback(0.7);

    public static readonly double TopP = "LLM_TOP_P"
        .GetEnvironmentValueWithFallback(0.8);

    public static readonly int TopK = "LLM_TOP_K"
        .GetEnvironmentValueWithFallback(20);

    public static readonly double MinP = "LLM_MIN_P"
        .GetEnvironmentValueWithFallback(0.0);

    public static readonly int DiscoverySeconds = "LLM_DISCOVERY_SECONDS"
        .GetEnvironmentValueWithFallback(15);

    public static readonly int SlotPollMilliseconds = "LLM_SLOT_POLL_MILLISECONDS"
        .GetEnvironmentValueWithFallback(1000);
}