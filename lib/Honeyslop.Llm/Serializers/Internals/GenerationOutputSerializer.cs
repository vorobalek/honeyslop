using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Honeyslop.Common.Serializers;
using Honeyslop.Llm.Contracts;

namespace Honeyslop.Llm.Serializers.Internals;

internal sealed class GenerationOutputSerializer : IJsonSerializer<GenerationOutputDto>
{
    private const int MaxPayloadBytes = 7900;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public string Serialize(GenerationOutputDto value)
    {
        var output = value with
        {
            Content = value.Content.Replace("\0", string.Empty, StringComparison.Ordinal)
        };
        while (true)
        {
            var payload = JsonSerializer.Serialize(output, SerializerOptions);
            if (Encoding.UTF8.GetByteCount(payload) <= MaxPayloadBytes)
                return payload;

            var start = Math.Max(1, output.Content.Length / 4);
            if (start < output.Content.Length && char.IsLowSurrogate(output.Content[start]))
                start++;
            output = output with
            {
                Content = output.Content[Math.Min(start, output.Content.Length)..],
                Truncated = true
            };
        }
    }

    public GenerationOutputDto? Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<GenerationOutputDto>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
