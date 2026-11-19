using Honeyslop.Admin.Access;
using Microsoft.AspNetCore.Http;

namespace Honeyslop.Admin.Tests.Access;

[TestClass]
public sealed class HttpRequestExtensionsTests
{
    [TestMethod]
    [DataRow("https", "https://honeyslop.example", null, true)]
    [DataRow("https", "https://HONEYSLOP.example", null, true)]
    [DataRow("wss", "https://honeyslop.example", null, true)]
    [DataRow("https", "https://evil.example", null, false)]
    [DataRow("wss", "https://evil.example", null, false)]
    [DataRow("https", "http://honeyslop.example", null, false)]
    [DataRow("ws", "https://honeyslop.example", null, false)]
    [DataRow("https", "https://honeyslop.example:8443", null, false)]
    [DataRow("https", "null", null, false)]
    [DataRow("https", "null", "same-origin", true)]
    [DataRow("https", "null", "cross-site", false)]
    [DataRow("https", "null", "none", false)]
    [DataRow("https", null, "same-origin", true)]
    [DataRow("https", null, "cross-site", false)]
    [DataRow("https", null, null, false)]
    public void IsSameOrigin_TrustsOnlyThisSite(string scheme, string? origin, string? fetchSite, bool expected)
    {
        var request = new DefaultHttpContext().Request;
        request.Scheme = scheme;
        request.Host = new HostString("honeyslop.example");
        if (origin is not null)
            request.Headers.Origin = origin;
        if (fetchSite is not null)
            request.Headers["Sec-Fetch-Site"] = fetchSite;

        Assert.AreEqual(expected, request.IsSameOrigin);
    }
}
