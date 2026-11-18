using System.Text.Json;

namespace Honeyslop.Admin.Core.Clients.Internals;

internal sealed class RipeStatClient(HttpClient httpClient, IUserAgentProvider userAgentProvider) : IRipeStatClient
{
    public static readonly Uri BaseAddress = new("https://stat.ripe.net/data/");

    public static void Configure(HttpClient client)
    {
        client.BaseAddress = BaseAddress;
        client.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<JsonElement> GetAsync(string dataCall, string resource, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{dataCall}/data.json?resource={Uri.EscapeDataString(resource)}&sourceapp=honeyslop");
        request.Headers.TryAddWithoutValidation("User-Agent", await userAgentProvider.GetAsync(cancellationToken));
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.TryGetProperty("data", out var data)
            ? data.Clone()
            : throw new JsonException($"RIPEstat {dataCall} answered without data");
    }
}
