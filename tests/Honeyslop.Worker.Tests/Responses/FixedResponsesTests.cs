using System.Text.RegularExpressions;
using Honeyslop.Worker.Llm;
using Honeyslop.Worker.Responses;

namespace Honeyslop.Worker.Tests.Responses;

[TestClass]
public sealed class FixedResponsesTests
{
    [TestMethod]
    public void Respond_GitHead_IsARefToABranch()
    {
        var response = FixedResponses.Respond("shop.test", "/.git/HEAD", File("nginx/1.16.1"), true);

        Assert.IsNotNull(response);
        Assert.AreEqual(200, response.Status);
        StringAssert.Matches(response.Body, new Regex("^ref: refs/heads/(master|main)\n$"));
        Assert.AreEqual("application/octet-stream", Header(response, "Content-Type"));
        Assert.AreEqual("nginx/1.16.1", Header(response, "Server"));
    }

    [TestMethod]
    [DataRow("/.git/refs/heads/live")]
    [DataRow("/app/.git/refs/remotes/origin/feature/login")]
    [DataRow("/.git/ORIG_HEAD")]
    public void Respond_GitRef_IsAStableCommitHash(string target)
    {
        var first = FixedResponses.Respond("shop.test", target, File("Apache/2.4.29 (Ubuntu)"), true);
        var second = FixedResponses.Respond("SHOP.test", target, File("Apache/2.4.29 (Ubuntu)"), true);

        Assert.IsNotNull(first);
        Assert.IsNotNull(second);
        StringAssert.Matches(first.Body, new Regex("^[0-9a-f]{40}\n$"));
        Assert.AreEqual(first.Body, second.Body);
    }

    [TestMethod]
    public void Respond_GitRefsOfDifferentPaths_Differ()
    {
        var live = FixedResponses.Respond("shop.test", "/.git/refs/heads/live", File("nginx/1.16.1"), true);
        var main = FixedResponses.Respond("shop.test", "/.git/refs/heads/main", File("nginx/1.16.1"), true);

        Assert.AreNotEqual(live!.Body, main!.Body);
    }

    [TestMethod]
    public void NotFound_Iis_IsItsOwnErrorPage()
    {
        var response = FixedResponses.NotFound("Microsoft-IIS/8.5", "shop.test", "/.DS_Store");

        Assert.AreEqual(404, response!.Status);
        StringAssert.Contains(response.Body, "<title>404 - File or directory not found.</title>");
        Assert.AreEqual("ASP.NET", Header(response, "X-Powered-By"));
    }

    [TestMethod]
    [DataRow("/.git/config")]
    [DataRow("/.git/head")]
    [DataRow("/.env")]
    public void Respond_OtherServedFile_IsLeftToTheModel(string target)
    {
        Assert.IsNull(FixedResponses.Respond("shop.test", target, File("nginx/1.16.1"), true));
    }

    [TestMethod]
    [DataRow("/.git/index")]
    [DataRow("/.DS_Store")]
    [DataRow("/backup/site.sqlite")]
    public void Respond_BinaryFile_IsNotFound(string target)
    {
        var response = FixedResponses.Respond("shop.test", target, new RequestProfile(ResponseKind.File, "x", "nginx/1.16.1", true), true);

        Assert.AreEqual(404, response!.Status);
        StringAssert.Contains(response.Body, "<center>nginx/1.16.1</center>");
    }

    [TestMethod]
    public void Respond_BinaryFileWithoutRefusals_IsLeftToTheModel()
    {
        Assert.IsNull(FixedResponses.Respond("shop.test", "/.git/index", new RequestProfile(ResponseKind.File, "x", "nginx/1.16.1", true), false));
    }

    [TestMethod]
    public void Respond_NotAFile_IsLeftToTheModel()
    {
        Assert.IsNull(FixedResponses.Respond("shop.test", "/.git/HEAD", new RequestProfile(ResponseKind.Exploit, "x", "nginx/1.16.1"), true));
    }

    [TestMethod]
    public void NotFound_Nginx_IsItsOwnErrorPage()
    {
        var response = FixedResponses.NotFound("nginx/1.14.0 (Ubuntu)", "shop.test", "/m9.php");

        Assert.AreEqual(
            "<html>\r\n<head><title>404 Not Found</title></head>\r\n<body>\r\n<center><h1>404 Not Found</h1></center>\r\n<hr><center>nginx/1.14.0 (Ubuntu)</center>\r\n</body>\r\n</html>\r\n",
            response!.Body);
        Assert.AreEqual("text/html", Header(response, "Content-Type"));
    }

    [TestMethod]
    [DataRow("Apache/2.2.15 (CentOS)", "<p>The requested URL /a%20b/&lt;x&gt;.php was not found on this server.</p>")]
    [DataRow("Apache/2.4.29 (Ubuntu)", "<p>The requested URL /a%20b/&lt;x&gt;.php was not found on this server.</p>")]
    [DataRow("Apache/2.4.38 (Debian)", "<p>The requested URL was not found on this server.</p>")]
    [DataRow("Apache/2.4.46 (Win64) OpenSSL/1.1.1g PHP/7.4.11", "<p>The requested URL was not found on this server.</p>")]
    public void NotFound_Apache_NamesTheUrlBefore2438(string server, string line)
    {
        var response = FixedResponses.NotFound(server, "shop.test:8080", "/a%2520b/%3Cx%3E.php?id=1");

        StringAssert.Contains(response!.Body, line);
        StringAssert.Contains(response.Body, $"<address>{server} Server at shop.test Port 8080</address>");
        Assert.AreEqual("text/html; charset=iso-8859-1", Header(response, "Content-Type"));
    }

    [TestMethod]
    public void NotFound_ApacheWithoutPort_SaysPort80()
    {
        StringAssert.Contains(FixedResponses.NotFound("Apache/2.4.6 (CentOS) PHP/5.4.16", "85.10.202.87", "/x")!.Body, "Server at 85.10.202.87 Port 80</address>");
    }

    [TestMethod]
    public void NotFound_Lighttpd_IsItsOwnErrorPage()
    {
        StringAssert.Contains(FixedResponses.NotFound("lighttpd/1.4.45", "shop.test", "/x")!.Body, "<h1>404 - Not Found</h1>");
    }

    [TestMethod]
    [DataRow("LiteSpeed")]
    [DataRow("Boa/0.94.14rc21")]
    [DataRow("")]
    public void NotFound_UnknownServer_IsLeftToTheModel(string server)
    {
        Assert.IsNull(FixedResponses.NotFound(server, "shop.test", "/x"));
    }

    private static RequestProfile File(string server)
    {
        return new RequestProfile(ResponseKind.File, "the file", server);
    }

    private static string? Header(GeneratedResponse response, string name)
    {
        return response.Headers.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}