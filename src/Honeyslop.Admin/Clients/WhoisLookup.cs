using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Honeyslop.Admin.Clients;

internal sealed partial class WhoisLookup(
    RipeStatClient ripeStatClient,
    IDistributedCache cache,
    TimeProvider timeProvider,
    ILogger<WhoisLookup> logger) : IWhoisLookup
{
    public static readonly TimeSpan CacheDuration = TimeSpan.FromDays(7);

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = CacheDuration
    };

    public async Task<WhoisInfo> LookupAsync(string clientIp, CancellationToken cancellationToken)
    {
        var key = "whois:" + clientIp;
        if (await ReadAsync(key, cancellationToken) is { } cached)
            return cached;

        var prefixOverview = ripeStatClient.GetAsync("prefix-overview", clientIp, cancellationToken);
        var abuseContactFinder = ripeStatClient.GetAsync("abuse-contact-finder", clientIp, cancellationToken);
        var whois = ripeStatClient.GetAsync("whois", clientIp, cancellationToken);
        await Task.WhenAll(prefixOverview, abuseContactFinder, whois);

        var info = WhoisParser.Parse(await prefixOverview, await abuseContactFinder, await whois, timeProvider.GetUtcNow());
        await WriteAsync(key, info, cancellationToken);
        return info;
    }

    private async Task<WhoisInfo?> ReadAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var cached = await cache.GetAsync(key, cancellationToken);
            return cached is null ? null : JsonSerializer.Deserialize<WhoisInfo>(cached);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogCacheFailed(logger, exception);
            return null;
        }
    }

    private async Task WriteAsync(string key, WhoisInfo info, CancellationToken cancellationToken)
    {
        try
        {
            await cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(info), CacheOptions, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogCacheFailed(logger, exception);
        }
    }

    [LoggerMessage(LogLevel.Warning, "WHOIS cache is unavailable, looking up without it")]
    static partial void LogCacheFailed(ILogger<WhoisLookup> logger, Exception exception);
}
