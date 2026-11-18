using System.Text;
using System.Text.RegularExpressions;
using Honeyslop.Data.Entities;

namespace Honeyslop.Trap.Responses.Internals;

internal sealed partial class ResponseSanitizer : IResponseSanitizer
{
    private const string DefaultContentType = "text/html; charset=utf-8";

    private static readonly HashSet<string> DroppedHeaders = new(StringComparer.OrdinalIgnoreCase)
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
    };

    public GeneratedResponse Sanitize(GeneratedResponse response, string host)
    {
        var status = response.Status is >= 200 and <= 599
            ? response.Status
            : StatusCodes.Status200OK;

        var headers = new List<ResponseHeader>();
        foreach (var header in response.Headers)
        {
            if (headers.Count >= Entry.ResponseHeadersMaxCount)
                break;

            var name = header.Name.Trim();
            if (!HeaderNameRegex().IsMatch(name) || DroppedHeaders.Contains(name))
                continue;

            var value = SanitizeValue(header.Value);
            if (name.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                value = CookieDomainRegex().Replace(value, string.Empty);
            else if (headers.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                continue;

            if (name.Equals("Location", StringComparison.OrdinalIgnoreCase))
                value = SanitizeLocation(value, host);

            headers.Add(new ResponseHeader
            {
                Name = name,
                Value = value
            });
        }

        var contentType = headers.FirstOrDefault(x => x.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));
        if (contentType is null || string.IsNullOrWhiteSpace(contentType.Value))
        {
            headers.RemoveAll(x => x.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));
            headers.Add(new ResponseHeader
            {
                Name = "Content-Type",
                Value = DefaultContentType
            });
        }
        else if (IsTextual(contentType.Value) && !contentType.Value.Contains("charset", StringComparison.OrdinalIgnoreCase))
        {
            contentType.Value += "; charset=utf-8";
        }

        var body = status is StatusCodes.Status204NoContent or StatusCodes.Status304NotModified
            ? string.Empty
            : UnescapeOverEscaped(response.Body.Replace("\0", string.Empty));
        if (body.Length > Entry.ResponseBodyMaxLength)
            body = body[..Entry.ResponseBodyMaxLength];

        return new GeneratedResponse(status, headers, body);
    }

    private static string UnescapeOverEscaped(string body)
    {
        body = body.Replace("\\$", "$");
        return body.Contains('\n') || !body.Contains("\\n", StringComparison.Ordinal)
            ? body
            : body
                .Replace("\\r\\n", "\n")
                .Replace("\\n", "\n")
                .Replace("\\t", "\t");
    }

    private static string SanitizeValue(string value)
    {
        var builder = new StringBuilder(Math.Min(value.Length, ResponseHeader.ValueMaxLength));
        foreach (var symbol in value)
        {
            if (builder.Length >= ResponseHeader.ValueMaxLength)
                break;
            if (symbol is '\t' or >= ' ' and <= '~')
                builder.Append(symbol);
        }

        return builder.ToString().Trim();
    }

    private static string SanitizeLocation(string value, string host)
    {
        if (value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal) && !value.StartsWith("/\\", StringComparison.Ordinal))
            return value;

        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
               uri.Host.Equals(host, StringComparison.OrdinalIgnoreCase)
            ? value
            : "/";
    }

    private static bool IsTextual(string contentType)
    {
        return contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
               contentType.Contains("javascript", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"^[!#$%&'*+\-.^_`|~0-9A-Za-z]+$")]
    private static partial Regex HeaderNameRegex();

    [GeneratedRegex(@";\s*domain\s*=[^;]*", RegexOptions.IgnoreCase)]
    private static partial Regex CookieDomainRegex();
}
