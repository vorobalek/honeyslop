using System.Text.Json;

namespace Honeyslop.Admin.Clients;

internal sealed class RipeStatClient(HttpClient httpClient)
{
    public static readonly Uri BaseAddress = new("https://stat.ripe.net/data/");

    public async Task<JsonElement> GetAsync(string dataCall, string resource, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"{dataCall}/data.json?resource={Uri.EscapeDataString(resource)}&sourceapp=honeyslop",
            cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.TryGetProperty("data", out var data)
            ? data.Clone()
            : throw new JsonException($"RIPEstat {dataCall} answered without data");
    }
}
