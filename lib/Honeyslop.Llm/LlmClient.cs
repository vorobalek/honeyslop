using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Honeyslop.Common;
using Microsoft.Extensions.Options;

namespace Honeyslop.Llm;

internal sealed class LlmClient(
    IHttpClientFactory httpClientFactory,
    IOptions<LlmOptions> options) : ILlmClient
{
    public const string HttpClientName = "llm";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private static readonly JsonNode ResponseSchema = JsonNode.Parse(
        """
        {
          "type": "object",
          "properties": {
            "status": { "type": "integer" },
            "headers": { "type": "object", "additionalProperties": { "type": "string" } },
            "body": { "type": "string" }
          },
          "required": ["status", "headers", "body"],
          "additionalProperties": false
        }
        """)!;

    public async Task<int?> GetIdleSlotsAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Value.ProbeTimeout);
        try
        {
            using var response = await httpClientFactory
                .CreateClient(HttpClientName)
                .GetAsync(new Uri(endpoint, "slots"), timeout.Token);
            if (!response.IsSuccessStatusCode)
                return null;

            var slots = await response.Content.ReadFromJsonAsync<SlotState[]>(SerializerOptions, timeout.Token);
            return slots?.Count(x => !x.IsProcessing);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    public async Task<LlmServerStatus?> GetStatusAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Value.ProbeTimeout);
        var httpClient = httpClientFactory.CreateClient(HttpClientName);
        try
        {
            var slots = await httpClient.GetFromJsonAsync<SlotState[]>(new Uri(endpoint, "slots"), SerializerOptions, timeout.Token) ?? [];
            var models = await httpClient.GetFromJsonAsync<ModelList>(new Uri(endpoint, "v1/models"), SerializerOptions, timeout.Token);
            var metrics = ParseMetrics(await httpClient.GetStringAsync(new Uri(endpoint, "metrics"), timeout.Token));
            var model = models?.Data?.FirstOrDefault();

            double? Rate(string tokens, string seconds)
            {
                return metrics.GetValueOrDefault(seconds) is > 0 and var elapsed
                    ? metrics.GetValueOrDefault(tokens) / elapsed
                    : null;
            }

            return new LlmServerStatus(
                model?.Id,
                model?.Meta?.NParams,
                model?.Meta?.Size,
                slots.Length,
                slots.Count(x => x.IsProcessing),
                slots.Select(x => x.NCtx).FirstOrDefault(),
                (long)metrics.GetValueOrDefault("llamacpp:prompt_tokens_total"),
                (long)metrics.GetValueOrDefault("llamacpp:prompt_tokens_cached_total"),
                (long)metrics.GetValueOrDefault("llamacpp:tokens_predicted_total"),
                Rate("llamacpp:prompt_tokens_total", "llamacpp:prompt_seconds_total"),
                Rate("llamacpp:tokens_predicted_total", "llamacpp:tokens_predicted_seconds_total"),
                (int)metrics.GetValueOrDefault("llamacpp:requests_processing"),
                (int)metrics.GetValueOrDefault("llamacpp:requests_deferred"));
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    public async Task<LlmCompletion> CompleteAsync(Uri endpoint, LlmPrompt prompt, TimeSpan timeLimit, CancellationToken cancellationToken)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeLimit > TimeSpan.Zero)
            limit.CancelAfter(timeLimit);
        try
        {
            return await CompleteWithinAsync(endpoint, prompt, limit.Token);
        }
        catch (OperationCanceledException) when (limit.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new LlmException($"Generation on {endpoint.Authority} exceeded the time limit of {timeLimit}.");
        }
    }

    private async Task<LlmCompletion> CompleteWithinAsync(Uri endpoint, LlmPrompt prompt, CancellationToken cancellationToken)
    {
        var request = new ChatCompletionRequest(
            options.Value.Model,
            [
                new ChatMessage("system", prompt.System),
                new ChatMessage("user", prompt.User)
            ],
            options.Value.Temperature,
            options.Value.MaxTokens,
            new ChatResponseFormat(
                "json_schema",
                new ChatJsonSchema("http_response", true, ResponseSchema.DeepClone())),
            false);

        HttpResponseMessage response;
        try
        {
            response = await httpClientFactory
                .CreateClient(HttpClientName)
                .PostAsJsonAsync(
                    new Uri(endpoint, "v1/chat/completions"),
                    request,
                    SerializerOptions,
                    cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new LlmUnavailableException($"Model server {endpoint.Authority} is unreachable: {exception.Message}", exception);
        }

        using var _ = response;
        if ((int)response.StatusCode >= 500 || response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            throw new LlmUnavailableException($"Model server {endpoint.Authority} responded {(int)response.StatusCode}.");
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new LlmException($"Model server {endpoint.Authority} responded {(int)response.StatusCode}: {error.Truncate(500)}");
        }

        ChatCompletionResponse? completion;
        try
        {
            completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(SerializerOptions, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new LlmUnavailableException($"Model server {endpoint.Authority} dropped the response: {exception.Message}", exception);
        }

        if (completion is null)
            throw new LlmException("Model server returned an empty body.");
        var choice = completion.Choices?.FirstOrDefault()
                     ?? throw new LlmException("Model server returned no choices.");
        return new LlmCompletion(
            choice.Message?.Content ?? string.Empty,
            choice.FinishReason,
            completion.Model,
            completion.Usage?.PromptTokens,
            completion.Usage?.CompletionTokens);
    }

    private static Dictionary<string, double> ParseMetrics(string text)
    {
        var metrics = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith('#'))
                continue;
            var separator = line.LastIndexOf(' ');
            if (separator > 0 && double.TryParse(line[(separator + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                metrics[line[..separator]] = value;
        }

        return metrics;
    }

    private sealed record SlotState(int Id, bool IsProcessing, int NCtx);

    private sealed record ModelList(IReadOnlyList<ModelInfo>? Data);

    private sealed record ModelInfo(string? Id, ModelMeta? Meta);

    private sealed record ModelMeta(long? NParams, long? Size);
}
