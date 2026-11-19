using Honeyslop.Admin.Access;
using Microsoft.AspNetCore.Http;

namespace Honeyslop.Admin.Tests.Access;

[TestClass]
public sealed class HttpRequestExtensionsTests
{
    [TestMethod]
    [DataRow("https://honeyslop.example", null, true)]
    [DataRow("https://HONEYSLOP.example", null, true)]
    [DataRow("https://evil.example", null, false)]
    [DataRow("http://honeyslop.example", null, false)]
    [DataRow("https://honeyslop.example:8443", null, false)]
    [DataRow("null", null, false)]
    [DataRow(null, "same-origin", true)]
    [DataRow(null, "cross-site", false)]
    [DataRow(null, null, false)]
    public void IsSameOrigin_TrustsOnlyThisSite(string? origin, string? fetchSite, bool expected)
    {
        var request = new DefaultHttpContext().Request;
        request.Scheme = "https";
        request.Host = new HostString("honeyslop.example");
        if (origin is not null)
            request.Headers.Origin = origin;
        if (fetchSite is not null)
            request.Headers["Sec-Fetch-Site"] = fetchSite;

        Assert.AreEqual(expected, request.IsSameOrigin);
    }
}
