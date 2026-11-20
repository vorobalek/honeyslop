using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Honeyslop.Common;
using Honeyslop.Data.Entities;
using Honeyslop.Llm.Exceptions;

namespace Honeyslop.Worker.Responses.Internals;

internal sealed partial class ResponseSanitizer : IResponseSanitizer
{
    private const string DefaultContentType = "text/html; charset=utf-8";

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
            if (!HeaderNameRegex().IsMatch(name) || ReservedResponseHeaders.Contains(name))
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
        Refuse(body);
        body = OnOwnHost(body, host, IsHtml(headers.First(x => x.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)).Value));
        if (body.Length > Entry.ResponseBodyMaxLength)
            body = body[..Entry.ResponseBodyMaxLength];

        return new GeneratedResponse(status, headers, body);
    }

    private static void Refuse(string body)
    {
        if (PlaceholderRegex().Match(body) is { Success: true } placeholder)
            throw new LlmException($"Model output has a placeholder: {placeholder.Value}");
        if (DisclosureRegex().Match(body) is { Success: true } disclosure)
            throw new LlmException($"Model output gives itself away: {disclosure.Value}");
    }

    private static string OnOwnHost(string body, string host, bool html)
    {
        var own = HostName(host);
        var onIp = IPAddress.TryParse(own, out _);
        if (own.Length > 0)
        {
            body = ReservedDomainRegex().Replace(body, onIp ? "localdomain" : own);
            if (onIp)
                body = DocumentationAddressRegex().Replace(body, own);
        }

        if (!html)
            return body;

        string Local(Match match)
        {
            if (HostName(match.Groups["host"].Value).Equals(own, StringComparison.OrdinalIgnoreCase))
                return match.Value;
            var rest = match.Groups["rest"].Value;
            return match.Groups["prefix"].Value + (rest.StartsWith('/') ? rest : "/" + rest);
        }

        return ForeignCssUrlRegex().Replace(ForeignAttributeRegex().Replace(body, Local), Local);
    }

    private static string HostName(string host)
    {
        var value = host.Trim();
        if (value.StartsWith('['))
            return value.TrimStart('[').Split(']')[0];
        var colon = value.LastIndexOf(':');
        return colon > 0 && value.IndexOf(':') == colon ? value[..colon] : value;
    }

    private static bool IsHtml(string contentType)
    {
        return contentType.Contains("html", StringComparison.OrdinalIgnoreCase);
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

    [GeneratedRegex(@"lorem ipsum|\byour[_-](?:[a-z]+[_-])*here\b|<\s*(?:placeholder|insert[^>]{0,40})\s*>|\[(?:insert|your) [^\]]{1,40}\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();

    [GeneratedRegex(@"\bhoneypots?\b|\blanguage model\b|\bas an ai\b|\bai[- ]generated\b|\bgenerated by (?:an )?ai\b|\bsimulated (?:response|server|output)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DisclosureRegex();

    [GeneratedRegex(@"\b(?:[a-z0-9-]+\.)*(?:example\.(?:com|org|net)|[a-z0-9-]+\.(?:test|example|invalid))\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReservedDomainRegex();

    [GeneratedRegex(@"\b(?:192\.0\.2|198\.51\.100|203\.0\.113)\.\d{1,3}\b", RegexOptions.CultureInvariant)]
    private static partial Regex DocumentationAddressRegex();

    [GeneratedRegex(@"(?<prefix>\b(?:src|href|action|poster|data|url)\s*=\s*[""']?)(?:https?:)?//(?<host>[^/""'\s>?#;]+)(?<rest>[^""'\s>;]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ForeignAttributeRegex();

    [GeneratedRegex(@"(?<prefix>\burl\(\s*[""']?)(?:https?:)?//(?<host>[^/""'\s)]+)(?<rest>[^""'\s)]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ForeignCssUrlRegex();
}