using System.Collections.Frozen;

namespace Honeyslop.Common;

public static class ReservedResponseHeaders
{
    private static readonly FrozenSet<string> Names = new[]
    {
        "Access-Control-Allow-Credentials",
        "Alt-Svc",
        "Clear-Site-Data",
        "Connection",
        "Content-Encoding",
        "Content-Length",
        "Content-Security-Policy",
        "Content-Security-Policy-Report-Only",
        "Date",
        "Keep-Alive",
        "Link",
        "Proxy-Authenticate",
        "Proxy-Connection",
        "Refresh",
        "Strict-Transport-Security",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade",
        "X-Content-Type-Options"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool Contains(string name)
    {
        return Names.Contains(name);
    }
}
