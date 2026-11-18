using System.Text.Json;
using Honeyslop.Data.Repositories;

namespace Honeyslop.Admin.Clients;

internal sealed partial class WhoisLookup(
    IRipeStatClient ripeStatClient,
    IClientWhoisRepository repository,
    TimeProvider timeProvider,
    ILogger<WhoisLookup> logger) : IWhoisLookup
{
    public static readonly TimeSpan RefreshAfter = TimeSpan.FromHours(12);

    public async Task<WhoisInfo> LookupAsync(string clientIp, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var stored = await repository.GetOrDefaultAsync(clientIp, cancellationToken);
        var cached = stored is null ? null : Deserialize(stored.Info);
        if (cached is not null && stored!.UpdatedAt > now - RefreshAfter)
            return cached;

        try
        {
            var prefixOverview = ripeStatClient.GetAsync("prefix-overview", clientIp, cancellationToken);
            var abuseContactFinder = ripeStatClient.GetAsync("abuse-contact-finder", clientIp, cancellationToken);
            var whois = ripeStatClient.GetAsync("whois", clientIp, cancellationToken);
            await Task.WhenAll(prefixOverview, abuseContactFinder, whois);

            var info = WhoisParser.Parse(await prefixOverview, await abuseContactFinder, await whois, now);
            await repository.SaveAsync(clientIp, JsonSerializer.Serialize(info), now, cancellationToken);
            return info;
        }
        catch (Exception exception) when (cached is not null &&
                                          exception is HttpRequestException or JsonException or TaskCanceledException &&
                                          !cancellationToken.IsCancellationRequested)
        {
            LogRefreshFailed(logger, clientIp, exception);
            return cached;
        }
    }

    private static WhoisInfo? Deserialize(string info)
    {
        try
        {
            return JsonSerializer.Deserialize<WhoisInfo>(info);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [LoggerMessage(LogLevel.Warning, "Could not refresh the WHOIS of {clientIp}, showing the stored one")]
    static partial void LogRefreshFailed(ILogger<WhoisLookup> logger, string clientIp, Exception exception);
}
