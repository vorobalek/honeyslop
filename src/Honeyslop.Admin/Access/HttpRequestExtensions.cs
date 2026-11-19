namespace Honeyslop.Admin.Access;

public static class HttpRequestExtensions
{
    extension(HttpRequest request)
    {
        public bool IsSameOrigin => request.Headers.Origin.ToString() is { Length: > 0 } origin
            ? Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
              string.Equals(uri.Scheme, request.Scheme, StringComparison.OrdinalIgnoreCase) &&
              string.Equals(uri.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase)
            : request.Headers["Sec-Fetch-Site"] == "same-origin";
    }
}
