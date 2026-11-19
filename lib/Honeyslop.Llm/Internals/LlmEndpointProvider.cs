using System.Net;
using System.Net.Sockets;
using Honeyslop.Llm.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Honeyslop.Llm.Internals;

internal sealed partial class LlmEndpointProvider(
    IOptions<LlmOptions> options,
    TimeProvider timeProvider,
    ILogger<LlmEndpointProvider> logger) : ILlmEndpointProvider
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private IReadOnlyList<LlmEndpoint> _endpoints = [];
    private DateTimeOffset _refreshedAt = DateTimeOffset.MinValue;

    public async ValueTask<IReadOnlyList<LlmEndpoint>> GetEndpointsAsync(CancellationToken cancellationToken)
    {
        if (timeProvider.GetUtcNow() - _refreshedAt < options.Value.DiscoveryInterval)
            return _endpoints;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (timeProvider.GetUtcNow() - _refreshedAt >= options.Value.DiscoveryInterval)
                await RefreshAsync(cancellationToken);
            return _endpoints;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var endpoints = new List<LlmEndpoint>();
        foreach (var url in options.Value.Urls)
            try
            {
                var literal = IPAddress.TryParse(url.Host, out var parsed);
                var addresses = literal
                    ? [parsed!]
                    : await Dns.GetHostAddressesAsync(url.Host, cancellationToken);
                var preferred = addresses.Any(x => x.AddressFamily == AddressFamily.InterNetwork)
                    ? addresses.Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                    : addresses;
                foreach (var address in preferred)
                {
                    var endpoint = new UriBuilder(url)
                    {
                        Host = address.ToString()
                    }.Uri;
                    var name = literal ? null : await NameOfAsync(address, cancellationToken);
                    endpoints.Add(new LlmEndpoint(
                        name ?? _endpoints.FirstOrDefault(x => x.Url == endpoint)?.Name ?? endpoint.Authority,
                        endpoint));
                }
            }
            catch (Exception exception) when (exception is SocketException or ArgumentException)
            {
                endpoints.AddRange(_endpoints.Where(x => x.Url.Port == url.Port && x.Url.AbsolutePath == url.AbsolutePath));
                LogDiscoveryFailed(logger, exception, url.Host);
            }

        var discovered = endpoints
            .Distinct()
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .ThenBy(x => x.Url.Authority, StringComparer.Ordinal)
            .ToArray();
        if (!discovered.SequenceEqual(_endpoints))
            LogEndpoints(logger, string.Join(", ", discovered.Select(x => $"{x.Name} ({x.Url.Authority})")));
        _endpoints = discovered;
        _refreshedAt = timeProvider.GetUtcNow();
    }

    private static async Task<string?> NameOfAsync(IPAddress address, CancellationToken cancellationToken)
    {
        try
        {
            var host = (await Dns.GetHostEntryAsync(address.ToString(), cancellationToken)).HostName;
            var name = host.Split('.')[0];
            return name.Length == 0 || IPAddress.TryParse(host, out _) ? null : name;
        }
        catch (Exception exception) when (exception is SocketException or ArgumentException)
        {
            return null;
        }
    }

    [LoggerMessage(LogLevel.Information, "Model servers: {endpoints}")]
    static partial void LogEndpoints(ILogger<LlmEndpointProvider> logger, string endpoints);

    [LoggerMessage(LogLevel.Warning, "Failed to resolve model servers behind {host}, keeping the previous list")]
    static partial void LogDiscoveryFailed(ILogger<LlmEndpointProvider> logger, Exception exception, string host);
}