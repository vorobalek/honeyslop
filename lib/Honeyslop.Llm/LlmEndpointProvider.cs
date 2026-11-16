using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Honeyslop.Llm;

internal sealed partial class LlmEndpointProvider(
    IOptions<LlmOptions> options,
    TimeProvider timeProvider,
    ILogger<LlmEndpointProvider> logger) : ILlmEndpointProvider
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private IReadOnlyList<Uri> _endpoints = [];
    private DateTimeOffset _refreshedAt = DateTimeOffset.MinValue;

    public async ValueTask<IReadOnlyList<Uri>> GetEndpointsAsync(CancellationToken cancellationToken)
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
        var endpoints = new List<Uri>();
        foreach (var url in options.Value.Urls)
            try
            {
                IPAddress[] addresses = IPAddress.TryParse(url.Host, out var literal)
                    ? [literal]
                    : await Dns.GetHostAddressesAsync(url.Host, cancellationToken);
                var preferred = addresses.Any(x => x.AddressFamily == AddressFamily.InterNetwork)
                    ? addresses.Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                    : addresses;
                endpoints.AddRange(preferred.Select(x => new UriBuilder(url)
                {
                    Host = x.ToString()
                }.Uri));
            }
            catch (Exception exception) when (exception is SocketException or ArgumentException)
            {
                endpoints.AddRange(_endpoints.Where(x => x.Port == url.Port && x.AbsolutePath == url.AbsolutePath));
                LogDiscoveryFailed(logger, exception, url.Host);
            }

        var discovered = endpoints
            .Distinct()
            .OrderBy(x => x.Authority, StringComparer.Ordinal)
            .ToArray();
        if (!discovered.SequenceEqual(_endpoints))
            LogEndpoints(logger, string.Join(", ", discovered.Select(x => x.Authority)));
        _endpoints = discovered;
        _refreshedAt = timeProvider.GetUtcNow();
    }

    [LoggerMessage(LogLevel.Information, "Model servers: {endpoints}")]
    static partial void LogEndpoints(ILogger<LlmEndpointProvider> logger, string endpoints);

    [LoggerMessage(LogLevel.Warning, "Failed to resolve model servers behind {host}, keeping the previous list")]
    static partial void LogDiscoveryFailed(ILogger<LlmEndpointProvider> logger, Exception exception, string host);
}
