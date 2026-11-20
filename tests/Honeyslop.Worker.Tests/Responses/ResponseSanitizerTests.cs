using Honeyslop.Data.Entities;
using Honeyslop.Llm.Exceptions;
using Honeyslop.Worker.Llm;
using Honeyslop.Worker.Responses;
using Honeyslop.Worker.Responses.Internals;

namespace Honeyslop.Worker.Tests.Responses;

[TestClass]
public sealed class ResponseSanitizerTests
{
    private const string Host = "kms.example.test";
    private static readonly RequestProfile SitePage = new(ResponseKind.Site, "the page /x of the website kms.example.test, served by nginx/1.18.0", "nginx/1.18.0");
    private readonly ResponseSanitizer _sanitizer = new();

    [TestMethod]
    public void Sanitize_DropsTransportAndSecurityHeaders()
    {
        var response = Sanitize(200, "body",
            ("Content-Length", "10"),
            ("Transfer-Encoding", "chunked"),
            ("Connection", "close"),
            ("Content-Security-Policy", "default-src *"),
            ("Alt-Svc", "h2=\"other.test:443\""),
            ("Refresh", "0; url=https://other.test"),
            ("Date", "Mon, 01 Jan 2001 00:00:00 GMT"),
            ("Server", "nginx/1.18.0"),
            ("Content-Type", "text/html"));

        CollectionAssert.AreEquivalent(new[]
        {
            "Server", "Content-Type"
        }, response.Headers.Select(x => x.Name).ToArray());
    }

    [TestMethod]
    public void Sanitize_DropsHeadersThatOnlyRequestsCarry()
    {
        var response = Sanitize(200, "body",
            ("Host", Host),
            ("User-Agent", "Mozilla/5.0"),
            ("Accept", "*/*"),
            ("Referer", "https://kms.example.test/"),
            ("X-Powered-By", "PHP/7.4.3"),
            ("Content-Type", "text/html"));

        CollectionAssert.AreEquivalent(new[]
        {
            "X-Powered-By", "Server", "Content-Type"
        }, response.Headers.Select(x => x.Name).ToArray());
    }

    [TestMethod]
    [DataRow("commit 4f1c2d3e5a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0d")]
    [DataRow("Call 1-800-123-4567 or +1 (234) 567-8901")]
    [DataRow("<p>For example, users can sign in with their e-mail.</p>")]
    public void Sanitize_RealisticValues_AreKept(string body)
    {
        Assert.AreEqual(body, Sanitize(200, body, ("Content-Type", "text/html")).Body);
    }

    [TestMethod]
    public void Sanitize_TextualContentTypeWithoutCharset_AddsUtf8()
    {
        var response = Sanitize(200, "{}", ("Content-Type", "application/json"));

        Assert.AreEqual("application/json; charset=utf-8", Header(response, "Content-Type"));
    }

    [TestMethod]
    public void Sanitize_MissingContentType_AddsHtml()
    {
        var response = Sanitize(200, "<html></html>", ("Server", "Apache"));

        Assert.AreEqual("text/html; charset=utf-8", Header(response, "Content-Type"));
    }

    [TestMethod]
    [DataRow("/login", "/login")]
    [DataRow("https://kms.example.test/login", "https://kms.example.test/login")]
    [DataRow("https://other.test/login", "/")]
    [DataRow("//other.test/login", "/")]
    [DataRow("/\\other.test", "/")]
    [DataRow("javascript:alert(1)", "/")]
    public void Sanitize_Location_StaysOnTheSameHost(string location, string expected)
    {
        var response = Sanitize(302, string.Empty, ("Location", location), ("Content-Type", "text/html"));

        Assert.AreEqual(expected, Header(response, "Location"));
    }

    [TestMethod]
    public void Sanitize_SetCookie_DropsDomainAndKeepsEveryCookie()
    {
        var response = Sanitize(200, string.Empty,
            ("Set-Cookie", "session=abc; Domain=.example.test; Path=/; HttpOnly"),
            ("Set-Cookie", "theme=dark; path=/"),
            ("Content-Type", "text/html"));

        var cookies = response.Headers.Where(x => x.Name == "Set-Cookie").Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[]
        {
            "session=abc; Path=/; HttpOnly", "theme=dark; path=/"
        }, cookies);
    }

    [TestMethod]
    public void Sanitize_HeaderValue_DropsControlAndNonAsciiCharacters()
    {
        var response = Sanitize(200, string.Empty, ("X-Powered-By", "PHP/8.1\r\nInjected: yes ✓"), ("Content-Type", "text/html"));

        Assert.AreEqual("PHP/8.1Injected: yes", Header(response, "X-Powered-By"));
    }

    [TestMethod]
    public void Sanitize_InvalidHeaderName_IsDropped()
    {
        var response = Sanitize(200, string.Empty, ("Bad Header", "x"), ("Content-Type", "text/html"));

        Assert.IsNull(Header(response, "Bad Header"));
    }

    [TestMethod]
    public void Sanitize_DuplicateHeaders_KeepsTheFirst()
    {
        var response = Sanitize(200, string.Empty, ("X-Powered-By", "PHP/7.4.3"), ("x-powered-by", "ASP.NET"), ("Content-Type", "text/html"));

        Assert.AreEqual("PHP/7.4.3", Header(response, "X-Powered-By"));
        Assert.AreEqual(1, response.Headers.Count(x => x.Name.Equals("X-Powered-By", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    [DataRow(101, 200)]
    [DataRow(99, 200)]
    [DataRow(700, 200)]
    [DataRow(418, 418)]
    public void Sanitize_Status_IsKeptInRange(int status, int expected)
    {
        var response = Sanitize(status, "x", ("Content-Type", "text/plain"));

        Assert.AreEqual(expected, response.Status);
    }

    [TestMethod]
    public void Sanitize_NoContent_DropsBody()
    {
        var response = Sanitize(204, "should not be sent");

        Assert.AreEqual(string.Empty, response.Body);
    }

    [TestMethod]
    public void Sanitize_BodyWithOnlyEscapedLineBreaks_IsUnescaped()
    {
        var response = Sanitize(200, "DB_HOST=db\\nDB_PASS=\\$2y$10$abc\\n\\tindented");

        Assert.AreEqual("DB_HOST=db\nDB_PASS=$2y$10$abc\n\tindented", response.Body);
    }

    [TestMethod]
    public void Sanitize_BodyWithRealLineBreaks_KeepsLiteralBackslashN()
    {
        var response = Sanitize(200, "printf(\"done\\n\");\nreturn 0;");

        Assert.AreEqual("printf(\"done\\n\");\nreturn 0;", response.Body);
    }

    [TestMethod]
    public void Sanitize_Body_DropsNulCharacters()
    {
        var response = Sanitize(200, "a\0b");

        Assert.AreEqual("ab", response.Body);
    }

    [TestMethod]
    public void Sanitize_Html_LoadsEverythingFromTheOwnHost()
    {
        var response = Sanitize(200,
            """<link href="https://cdn.jsdelivr.net/css/site.css"><script src="//cdn.jsdelivr.net/js/app.js"></script><a href="https://kms.example.test/login">in</a><img src="https://img.imgur.net"><style>body{background:url('https://img.imgur.net/bg.png')}</style><meta http-equiv="refresh" content="0; url=https://other-site.net/next">""",
            ("Content-Type", "text/html"));

        Assert.AreEqual(
            """<link href="/css/site.css"><script src="/js/app.js"></script><a href="https://kms.example.test/login">in</a><img src="/"><style>body{background:url('/bg.png')}</style><meta http-equiv="refresh" content="0; url=/next">""",
            response.Body);
    }

    [TestMethod]
    public void Sanitize_NotHtml_KeepsForeignUrls()
    {
        var response = Sanitize(200, "[remote \"origin\"]\n\turl = https://gitlab.other-site.net/web/shop.git", ("Content-Type", "text/plain"));

        StringAssert.Contains(response.Body, "url = https://gitlab.other-site.net/web/shop.git");
    }

    [TestMethod]
    public void Sanitize_ReservedNames_BecomeTheHost()
    {
        var response = _sanitizer.Sanitize(new GeneratedResponse(200, [], "MAIL_FROM=noreply@example.com\nAPI=https://api.example.org/v1\nGIT=gitlab.northwind.test\nPEER=198.51.100.23"), "shop.ru:8080", SitePage, true);

        Assert.AreEqual("MAIL_FROM=noreply@shop.ru\nAPI=https://shop.ru/v1\nGIT=shop.ru\nPEER=198.51.100.23", response.Body);
    }

    [TestMethod]
    public void Sanitize_ReservedNamesOnAnAddress_BecomeLocal()
    {
        var response = _sanitizer.Sanitize(new GeneratedResponse(200, [], "admin@northwind.test from 203.0.113.9"), "85.10.202.87", SitePage, true);

        Assert.AreEqual("admin@localhost from 85.10.202.87", response.Body);
    }

    [TestMethod]
    [DataRow("<p>Lorem ipsum dolor sit amet</p>")]
    [DataRow("API_KEY=your_api_key_here")]
    [DataRow("password=<placeholder>")]
    [DataRow("<!-- this honeypot page was generated -->")]
    [DataRow("As an AI language model I cannot")]
    [DataRow("This server is vulnerable to path traversal.")]
    [DataRow("Note: This is a demonstration server. No real data is stored.")]
    [DataRow("Do not attempt to access sensitive files directly.")]
    [DataRow("These are dummy credentials for testing purposes.")]
    [DataRow("session_id=a1b2c3d4e5f6a7b8")]
    [DataRow("Serial: SN1234567890")]
    [DataRow("MAC 00:11:22:33:44:55")]
    [DataRow("token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c")]
    [DataRow("<td>Password Example</td>")]
    [DataRow("<p>Example user: admin</p>")]
    public void Sanitize_PlaceholderOrGiveaway_IsRefused(string body)
    {
        Assert.ThrowsExactly<LlmException>(() => Sanitize(200, body, ("Content-Type", "text/html")));
    }

    [TestMethod]
    public void Sanitize_Server_IsTheOneTheProfileNames()
    {
        var boa = _sanitizer.Sanitize(new GeneratedResponse(200, [
            new ResponseHeader
            {
                Name = "Server",
                Value = "phpMyAdmin 4.9.12"
            }
        ], "ok"), Host, new RequestProfile(ResponseKind.App, "router", "Boa/0.94.14rc21"), true);
        var none = _sanitizer.Sanitize(new GeneratedResponse(200, [
            new ResponseHeader
            {
                Name = "Server",
                Value = "Spring Boot Actuator/2.7.12"
            }
        ], "{}"), Host, new RequestProfile(ResponseKind.Api, "Spring Boot Actuator", string.Empty), true);

        Assert.AreEqual("Boa/0.94.14rc21", boa.Headers.Single(x => x.Name == "Server").Value);
        Assert.IsFalse(none.Headers.Any(x => x.Name.Equals("Server", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    [DataRow(401)]
    [DataRow(403)]
    [DataRow(404)]
    public void Sanitize_RefusalWhereItMayNotRefuse_IsRefused(int status)
    {
        Assert.ThrowsExactly<LlmException>(() => _sanitizer.Sanitize(new GeneratedResponse(status, [], "Forbidden"), Host, new RequestProfile(ResponseKind.File, "x", "nginx/1.18.0"), false));
    }

    [TestMethod]
    public void Sanitize_RefusalWhereItMayRefuse_IsKept()
    {
        var response = _sanitizer.Sanitize(new GeneratedResponse(404, [], "Not Found"), Host, SitePage, true);

        Assert.AreEqual(404, response.Status);
    }

    private GeneratedResponse Sanitize(int status, string body, params (string Name, string Value)[] headers)
    {
        return _sanitizer.Sanitize(
            new GeneratedResponse(
                status,
                [
                    .. headers.Select(x => new ResponseHeader
                    {
                        Name = x.Name,
                        Value = x.Value
                    })
                ],
                body),
            Host,
            SitePage,
            true);
    }

    private static string? Header(GeneratedResponse response, string name)
    {
        return response.Headers.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}