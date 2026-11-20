using System.Net;
using System.Text.RegularExpressions;

namespace Honeyslop.Worker.Llm;

public static partial class RequestProfiler
{
    private static readonly (Regex Path, ResponseKind Kind, string Subject)[] Known =
    [
        (Path(@"^/hnap1"), ResponseKind.Device, "D-Link DIR-series home router, HNAP interface"),
        (Path(@"^/isapi/"), ResponseKind.Device, "Hikvision IP camera, ISAPI interface"),
        (Path(@"^/sdk/weblanguage"), ResponseKind.Device, "Hikvision IP camera"),
        (Path(@"^/onvif/"), ResponseKind.Device, "IP camera, ONVIF device service"),
        (Path(@"^/cgi-bin/luci"), ResponseKind.App, "OpenWrt router, LuCI web interface"),
        (Path(@"^/boaform/"), ResponseKind.App, "Realtek-based home router on the Boa web server"),
        (Path(@"^/goform/"), ResponseKind.App, "Tenda home router"),
        (Path(@"^/gponform/"), ResponseKind.App, "GPON fibre home router"),
        (Path(@"^/setup\.cgi"), ResponseKind.App, "Netgear home router"),
        (Path(@"^/webfig"), ResponseKind.App, "MikroTik RouterOS WebFig"),
        (Path(@"^/cgi-bin/"), ResponseKind.Device, "embedded Linux device with a CGI web interface"),
        (Path(@"^/sdk(/|$)"), ResponseKind.Api, "VMware ESXi SOAP API"),
        (Path(@"^/wp-admin/(setup-config|install)\.php"), ResponseKind.App, "WordPress installer, first step"),
        (Path(@"^/wp-content/plugins/(?<name>[^/]+)"), ResponseKind.App, "WordPress plugin {name}"),
        (Path(@"^/(wp-login\.php|wp-admin|wp-)"), ResponseKind.App, "WordPress 5"),
        (Path(@"^/xmlrpc\.php"), ResponseKind.Api, "WordPress XML-RPC endpoint"),
        (Path(@"^/(phpmyadmin|pma|myadmin)"), ResponseKind.App, "phpMyAdmin 4"),
        (Path(@"^/(manager|host-manager)/html"), ResponseKind.App, "Apache Tomcat Manager"),
        (Path(@"^/solr/admin/"), ResponseKind.Api, "Apache Solr admin API"),
        (Path(@"^/solr"), ResponseKind.App, "Apache Solr admin UI"),
        (Path(@"^/(jenkins|script$|scripttext$)"), ResponseKind.App, "Jenkins"),
        (Path(@"^/(owa|ecp)(/|$)"), ResponseKind.App, "Microsoft Exchange Outlook Web App"),
        (Path(@"^/autodiscover/"), ResponseKind.Api, "Microsoft Exchange Autodiscover"),
        (Path(@"^/remote/(login|fgt_lang)"), ResponseKind.App, "FortiGate SSL VPN portal"),
        (Path(@"^/\+cscoe\+/"), ResponseKind.App, "Cisco ASA AnyConnect portal"),
        (Path(@"^/dana-na/"), ResponseKind.App, "Pulse Secure VPN portal"),
        (Path(@"^/vpn/"), ResponseKind.App, "Citrix Gateway"),
        (Path(@"^/administrator"), ResponseKind.App, "Joomla 3 administrator"),
        (Path(@"^/(phpinfo|info|php_info|test)\.php"), ResponseKind.App, "phpinfo() page of PHP 7"),
        (Path(@"^/server-status"), ResponseKind.App, "Apache mod_status page"),
        (Path(@"^/actuator"), ResponseKind.Api, "Spring Boot Actuator"),
        (Path(@"^/v2/(_catalog|$)"), ResponseKind.Api, "Docker Registry HTTP API v2"),
        (Path(@"^/(containers/json|images/json|info$|version$)"), ResponseKind.Api, "Docker Engine API"),
        (Path(@"^/(api/v1/(pods|namespaces|nodes|secrets)|apis(/|$))"), ResponseKind.Api, "Kubernetes API server"),
        (Path(@"^/_(cat|cluster|nodes|search|all)"), ResponseKind.Api, "Elasticsearch 6"),
        (Path(@"^/graphql"), ResponseKind.Api, "GraphQL API of {host}")
    ];

    private static readonly Regex[] ExposedFiles =
    [
        Path(@"/\.(env|git/|git-credentials|aws/|ssh/|htpasswd|npmrc|docker/|kube/|bash_history)"),
        Path(@"/(wp-config|config|configuration|settings|database|db|secrets?|credentials|appsettings[^/]*)(\.(php|json|ya?ml|ini|xml|txt|bak|old|orig|save|swp|dist))+~?$"),
        Path(@"/docker-compose\.ya?ml$|/web\.config$|/\.ds_store$")
    ];

    private static readonly string[] Servers =
    [
        "nginx/1.10.3 (Ubuntu)",
        "nginx/1.12.2",
        "nginx/1.14.0 (Ubuntu)",
        "nginx/1.14.2",
        "nginx/1.16.1",
        "nginx/1.17.10",
        "nginx/1.20.1",
        "openresty/1.15.8.3",
        "Apache/2.2.15 (CentOS)",
        "Apache/2.4.6 (CentOS) PHP/5.4.16",
        "Apache/2.4.18 (Ubuntu)",
        "Apache/2.4.25 (Debian)",
        "Apache/2.4.29 (Ubuntu)",
        "Apache/2.4.38 (Debian)",
        "Apache/2.4.46 (Win64) OpenSSL/1.1.1g PHP/7.4.11",
        "Microsoft-IIS/7.5",
        "Microsoft-IIS/8.5",
        "Microsoft-IIS/10.0",
        "LiteSpeed",
        "lighttpd/1.4.45"
    ];

    private static readonly Regex FileExtension = Path(@"\.(env|ini|conf|cfg|config|ya?ml|toml|properties|sql|bak|old|orig|save|swp|log|pem|key|sh|txt)$");

    public static RequestProfile Profile(string host, string target, string? body)
    {
        var questionMark = target.IndexOf('?');
        var path = Uri.UnescapeDataString(questionMark >= 0 ? target[..questionMark] : target).ToLowerInvariant();
        var onIp = IPAddress.TryParse(host, out _);
        var server = $"served by {ServerFor(host)}";
        var site = onIp ? $"the web server at {host}" : $"the website {host}";
        var known = Known
            .Select(x => (x.Kind, Match: x.Path.Match(path), x.Subject))
            .FirstOrDefault(x => x.Match.Success);
        var software = known.Match is { Success: true } match
            ? known.Subject.Replace("{name}", match.Groups["name"].Value).Replace("{host}", host)
            : null;
        var payload = Uri.UnescapeDataString(target) + "\n" + body;
        if (Exploit(payload) is { } exploit)
            return new RequestProfile(ResponseKind.Exploit, $"{software ?? $"{site}, {server}"}, where {exploit} succeeds");
        if (ExposedFiles.Any(x => x.IsMatch(path)))
            return new RequestProfile(ResponseKind.File, $"the file {path} on {site}, {server}");
        if (software is not null)
            return new RequestProfile(known.Kind, software);
        if (FileExtension.IsMatch(path))
            return new RequestProfile(ResponseKind.File, $"the file {path} on {site}, {server}");
        if (path.StartsWith("/api/", StringComparison.Ordinal) || path.StartsWith("/v1/", StringComparison.Ordinal) || path.StartsWith("/rest/", StringComparison.Ordinal))
            return new RequestProfile(ResponseKind.Api, $"the JSON API of {site}, {server}");
        if (path is "/admin" or "/login" or "/signin" || path.StartsWith("/admin/", StringComparison.Ordinal) || path.StartsWith("/user/login", StringComparison.Ordinal))
            return new RequestProfile(ResponseKind.App, $"the login page of {site}, {server}");
        if (path is "/" or "/index.html" or "/index.htm" or "/index.php" or "/home")
            return new RequestProfile(ResponseKind.Site, onIp ? $"the default page of a freshly installed web server at {host}, {server}" : $"the home page of the website {host}, {server}");
        return new RequestProfile(ResponseKind.Site, $"the page {path} of {site}, {server}");
    }

    public static string ServerFor(string host)
    {
        var hash = 2166136261u;
        foreach (var symbol in host.ToLowerInvariant())
            hash = (hash ^ symbol) * 16777619u;
        return Servers[hash % (uint)Servers.Length];
    }

    private static string? Exploit(string payload)
    {
        if (TraversalRegex().IsMatch(payload))
            return "the path traversal that reads a file";
        if (CommandRegex().IsMatch(payload))
            return "the injected shell command";
        if (JndiRegex().IsMatch(payload))
            return "the JNDI lookup injected into a logged value";
        if (SqlRegex().IsMatch(payload))
            return "the SQL injection";
        if (PhpRegex().IsMatch(payload))
            return "the injected PHP code";
        return null;
    }

    private static Regex Path(string pattern)
    {
        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }

    [GeneratedRegex(@"\.\./|\.\.\\|/etc/(passwd|shadow|hosts)\b|\bwin\.ini\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TraversalRegex();

    [GeneratedRegex(@"(;|\||&&|\$\(|`)\s*(wget|curl|id|uname|cat|echo|sh|bash|busybox|chmod|rm|nc|ping|whoami)\b|\b(cmd|exec|command)=", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CommandRegex();

    [GeneratedRegex(@"\$\{jndi:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex JndiRegex();

    [GeneratedRegex(@"\bunion\s+(all\s+)?select\b|'\s*or\s+'?1'?\s*=\s*'?1|\bsleep\(\d+\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SqlRegex();

    [GeneratedRegex(@"<\?php|\bphp://(input|filter)|\ballow_url_include\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PhpRegex();
}