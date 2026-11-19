namespace Honeyslop.Admin.Access;

public static class HttpRequestExtensions
{
    extension(HttpRequest request)
    {
        public bool IsSameOrigin => request.Headers.Origin.ToString() is { Length: > 0 } origin
            ? Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
              string.Equals(uri.Scheme, PageScheme(request.Scheme), StringComparison.OrdinalIgnoreCase) &&
              string.Equals(uri.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase)
            : request.Headers["Sec-Fetch-Site"] == "same-origin";
    }

    private static string PageScheme(string scheme)
    {
        return scheme.ToLowerInvariant() switch
        {
            "wss" => Uri.UriSchemeHttps,
            "ws" => Uri.UriSchemeHttp,
            _ => scheme
        };
    }
}
