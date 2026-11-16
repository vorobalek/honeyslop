using Honeyslop.Data.Entities;
using Honeyslop.Trap.Responses;

namespace Honeyslop.Trap.Tests.Responses;

[TestClass]
public sealed class ResponseSanitizerTests
{
    private const string Host = "kms.example.test";
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

        CollectionAssert.AreEquivalent(new[] { "Server", "Content-Type" }, response.Headers.Select(x => x.Name).ToArray());
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
        CollectionAssert.AreEqual(new[] { "session=abc; Path=/; HttpOnly", "theme=dark; path=/" }, cookies);
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
        var response = Sanitize(200, string.Empty, ("Server", "nginx"), ("server", "Apache"), ("Content-Type", "text/html"));

        Assert.AreEqual("nginx", Header(response, "Server"));
        Assert.AreEqual(1, response.Headers.Count(x => x.Name.Equals("Server", StringComparison.OrdinalIgnoreCase)));
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

    private GeneratedResponse Sanitize(int status, string body, params (string Name, string Value)[] headers)
    {
        return _sanitizer.Sanitize(
            new GeneratedResponse(
                status,
                [
                    ..headers.Select(x => new ResponseHeader
                    {
                        Name = x.Name,
                        Value = x.Value
                    })
                ],
                body),
            Host);
    }

    private static string? Header(GeneratedResponse response, string name)
    {
        return response.Headers.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}
