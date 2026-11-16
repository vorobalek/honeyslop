using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Honeyslop.Data.Repositories;

public sealed record GenerationOutput(
    long EntryId,
    int Tokens,
    string Content,
    bool Truncated = false)
{
    private const int MaxPayloadBytes = 7900;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public string ToPayload()
    {
        var output = this with
        {
            Content = Content.Replace("\0", string.Empty, StringComparison.Ordinal)
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

    public static GenerationOutput? FromPayload(string payload)
    {
        try
        {
            return JsonSerializer.Deserialize<GenerationOutput>(payload, SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
