using System.Text.Json;
using Honeyslop.Data.Entities;
using Honeyslop.Llm.Exceptions;
using Honeyslop.Worker.Responses;

namespace Honeyslop.Worker.Llm;

public static class LlmResponseParser
{
    public static GeneratedResponse Parse(string content)
    {
        var json = StripCodeFence(content.Trim());
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new LlmException($"Model output is not valid JSON: {exception.Message}");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new LlmException("Model output is not a JSON object.");

            if (!root.TryGetProperty("status", out var statusElement) ||
                statusElement.ValueKind != JsonValueKind.Number ||
                !statusElement.TryGetInt32(out var status))
                throw new LlmException("Model output has no integer 'status'.");

            if (!root.TryGetProperty("body", out var bodyElement) ||
                bodyElement.ValueKind != JsonValueKind.String)
                throw new LlmException("Model output has no string 'body'.");

            var headers = new List<ResponseHeader>();
            if (root.TryGetProperty("headers", out var headersElement) &&
                headersElement.ValueKind == JsonValueKind.Object)
                foreach (var header in headersElement.EnumerateObject())
                    if (header.Value.ValueKind == JsonValueKind.String)
                        headers.Add(new ResponseHeader
                        {
                            Name = header.Name,
                            Value = header.Value.GetString()!
                        });

            return new GeneratedResponse(status, headers, bodyElement.GetString()!);
        }
    }

    private static string StripCodeFence(string content)
    {
        if (!content.StartsWith("```", StringComparison.Ordinal))
            return content;

        var firstLineEnd = content.IndexOf('\n');
        var lastFence = content.LastIndexOf("```", StringComparison.Ordinal);
        return firstLineEnd >= 0 && lastFence > firstLineEnd
            ? content[(firstLineEnd + 1)..lastFence].Trim()
            : content;
    }
}