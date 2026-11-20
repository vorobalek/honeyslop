using System.Net;
using System.Text.RegularExpressions;

namespace Honeyslop.Worker.Llm;

public static partial class RequestProfiler
{
    private static readonly string[] WebServers =
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

    private static readonly string[] EmbeddedServers =
    [
        "Boa/0.94.14rc21",
        "lighttpd/1.4.35",
        "mini_httpd/1.19 19dec2003",
        "GoAhead-Webs",
        "thttpd/2.25b 29dec2003"
    ];

    private static readonly (Regex Path, ResponseKind Kind, string Subject, Func<string, string> Server)[] Known =
    [
        (Path(@"^/hnap1"), ResponseKind.Device, "D-Link DIR-850L home router, HNAP interface", _ => "Linux, HTTP/1.1, DIR-850L Ver 1.13"),
        (Path(@"^/isapi/"), ResponseKind.Device, "Hikvision IP camera, ISAPI interface", _ => "App-webs/"),
        (Path(@"^/sdk/weblanguage"), ResponseKind.Device, "Hikvision IP camera", _ => "App-webs/"),
        (Path(@"^/onvif/"), ResponseKind.Device, "IP camera, ONVIF device service", _ => "gSOAP/2.8"),
        (Path(@"^/cgi-bin/luci"), ResponseKind.App, "OpenWrt router, LuCI web interface", _ => string.Empty),
        (Path(@"^/boaform/"), ResponseKind.App, "Realtek-based home router web interface", _ => "Boa/0.94.14rc21"),
        (Path(@"^/goform/"), ResponseKind.App, "Tenda home router web interface", _ => "GoAhead-Webs"),
        (Path(@"^/gponform/"), ResponseKind.App, "GPON fibre home router web interface", _ => "Boa/0.93.15"),
        (Path(@"^/setup\.cgi"), ResponseKind.App, "Netgear home router web interface", _ => "mini_httpd/1.19 19dec2003"),
        (Path(@"^/webfig"), ResponseKind.App, "MikroTik RouterOS WebFig", _ => string.Empty),
        (Path(@"^/cgi-bin/"), ResponseKind.Device, "embedded Linux device with a CGI web interface", EmbeddedServer),
        (Path(@"^/sdk(/|$)"), ResponseKind.Api, "VMware ESXi SOAP API", _ => string.Empty),
        (Path(@"^/wp-admin/(setup-config|install)\.php"), ResponseKind.App, "WordPress installer, first step", WebServer),
        (Path(@"^/wp-content/plugins/(?<name>[^/]+)"), ResponseKind.App, "WordPress plugin {name}", WebServer),
        (Path(@"^/(wp-login\.php|wp-admin|wp-)"), ResponseKind.App, "WordPress 5", WebServer),
        (Path(@"^/xmlrpc\.php"), ResponseKind.Api, "WordPress XML-RPC endpoint", WebServer),
        (Path(@"^/(phpmyadmin|pma|myadmin)"), ResponseKind.App, "phpMyAdmin 4", WebServer),
        (Path(@"^/(manager|host-manager)/html"), ResponseKind.App, "Apache Tomcat Manager", _ => "Apache-Coyote/1.1"),
        (Path(@"^/solr/admin/"), ResponseKind.Api, "Apache Solr admin API", _ => "Jetty(8.1.10.v20130312)"),
        (Path(@"^/solr"), ResponseKind.App, "Apache Solr admin UI", _ => "Jetty(8.1.10.v20130312)"),
        (Path(@"^/(jenkins|script$|scripttext$)"), ResponseKind.App, "Jenkins", _ => "Jetty(9.4.z-SNAPSHOT)"),
        (Path(@"^/(owa|ecp)(/|$)"), ResponseKind.App, "Microsoft Exchange Outlook Web App", _ => "Microsoft-IIS/8.5"),
        (Path(@"^/autodiscover/"), ResponseKind.Api, "Microsoft Exchange Autodiscover", _ => "Microsoft-IIS/8.5"),
        (Path(@"^/remote/(login|fgt_lang)"), ResponseKind.App, "FortiGate SSL VPN portal", _ => "xxxxxxxx-xxxxx"),
        (Path(@"^/\+cscoe\+/"), ResponseKind.App, "Cisco ASA AnyConnect portal", _ => string.Empty),
        (Path(@"^/dana-na/"), ResponseKind.App, "Pulse Secure VPN portal", _ => string.Empty),
        (Path(@"^/vpn/"), ResponseKind.App, "Citrix Gateway", _ => string.Empty),
        (Path(@"^/administrator"), ResponseKind.App, "Joomla 3 administrator", WebServer),
        (Path(@"^/(phpinfo|info|php_info|test)\.php"), ResponseKind.App, "phpinfo() page of PHP 7", WebServer),
        (Path(@"^/server-status"), ResponseKind.App, "Apache mod_status page", ApacheServer),
        (Path(@"^/actuator"), ResponseKind.Api, "Spring Boot Actuator", _ => string.Empty),
        (Path(@"^/v2/(_catalog|$)"), ResponseKind.Api, "Docker Registry HTTP API v2", _ => string.Empty),
        (Path(@"^/(containers/json|images/json|info$|version$)"), ResponseKind.Api, "Docker Engine API", _ => "Docker/19.03.12 (linux)"),
        (Path(@"^/(api/v1/(pods|namespaces|nodes|secrets)|apis(/|$))"), ResponseKind.Api, "Kubernetes API server", _ => string.Empty),
        (Path(@"^/_(cat|cluster|nodes|search|all)"), ResponseKind.Api, "Elasticsearch 6", _ => string.Empty),
        (Path(@"^/graphql"), ResponseKind.Api, "GraphQL API of {host}", WebServer)
    ];

    private static readonly Regex[] ExposedFiles =
    [
        Path(@"/\.(env|git/|git-credentials|aws/|ssh/|htpasswd|npmrc|docker/|kube/|bash_history)"),
        Path(@"/(wp-config|config|configuration|settings|database|db|secrets?|credentials|appsettings[^/]*)(\.(php|json|ya?ml|ini|xml|txt|bak|old|orig|save|swp|dist))+~?$"),
        Path(@"/docker-compose\.ya?ml$|/web\.config$|/\.ds_store$")
    ];

    private static readonly Regex FileExtension = Path(@"\.(env|ini|conf|cfg|config|ya?ml|toml|properties|sql|bak|old|orig|save|swp|log|pem|key|sh|txt)$");

    public static RequestProfile Profile(string host, string target, string? body)
    {
        var questionMark = target.IndexOf('?');
        var path = Uri.UnescapeDataString(questionMark >= 0 ? target[..questionMark] : target).ToLowerInvariant();
        var onIp = IPAddress.TryParse(host, out _);
        var web = WebServer(host);
        var site = onIp ? $"the web server at {host}" : $"the website {host}";
        var known = Known
            .Select(x => (x.Kind, Match: x.Path.Match(path), x.Subject, x.Server))
            .FirstOrDefault(x => x.Match.Success);
        var software = known.Match is { Success: true } match
            ? known.Subject.Replace("{name}", match.Groups["name"].Value).Replace("{host}", host)
            : null;
        var server = software is null ? web : known.Server(host);
        if (Outcome(Uri.UnescapeDataString(target) + "\n" + body) is { } outcome)
            return new RequestProfile(ResponseKind.Exploit, $"{software ?? $"{site}, served by {web}"}, and {outcome}", server);
        if (ExposedFiles.Any(x => x.IsMatch(path)))
            return File(path, site, web);
        if (software is not null)
            return new RequestProfile(known.Kind, software, server);
        if (FileExtension.IsMatch(path))
            return File(path, site, web);
        if (path.StartsWith("/api/", StringComparison.Ordinal) || path.StartsWith("/v1/", StringComparison.Ordinal) || path.StartsWith("/rest/", StringComparison.Ordinal))
            return new RequestProfile(ResponseKind.Api, $"the JSON API of {site}, served by {web}", web);
        if (path is "/admin" or "/login" or "/signin" || path.StartsWith("/admin/", StringComparison.Ordinal) || path.StartsWith("/user/login", StringComparison.Ordinal))
            return new RequestProfile(ResponseKind.App, $"the login page of {site}, served by {web}", web);
        if (path is "/" or "/index.html" or "/index.htm" or "/index.php" or "/home")
            return new RequestProfile(ResponseKind.Site, onIp ? $"the default page of a freshly installed web server at {host}, served by {web}" : $"the home page of the website {host}, served by {web}", web);
        return new RequestProfile(ResponseKind.Site, $"the page {path} of {site}, served by {web}", web);
    }

    public static string WebServer(string host)
    {
        return WebServers[Hash(host) % (uint)WebServers.Length];
    }

    private static string EmbeddedServer(string host)
    {
        return EmbeddedServers[Hash(host) % (uint)EmbeddedServers.Length];
    }

    private static string ApacheServer(string host)
    {
        string[] apache = [.. WebServers.Where(x => x.StartsWith("Apache/", StringComparison.Ordinal))];
        return apache[Hash(host) % (uint)apache.Length];
    }

    private static uint Hash(string host)
    {
        var hash = 2166136261u;
        foreach (var symbol in host.ToLowerInvariant())
            hash = (hash ^ symbol) * 16777619u;
        return hash;
    }

    private static RequestProfile File(string path, string site, string server)
    {
        return new RequestProfile(ResponseKind.File, $"the file {path}, readable on {site}, served by {server}", server);
    }

    private static string? Outcome(string payload)
    {
        if (TraversalRegex().IsMatch(payload))
            return "the response is the raw content of the file the request names";
        if (CommandRegex().IsMatch(payload))
            return "the response contains the output of the shell command in the request";
        if (JndiRegex().IsMatch(payload))
            return "the response is its usual page";
        if (SqlRegex().IsMatch(payload))
            return "the response contains the database rows the SQL in the request selects";
        if (PhpRegex().IsMatch(payload))
            return "the response contains the output of the PHP code in the request";
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