using Honeyslop.Admin.Access;

namespace Honeyslop.Admin.Tests.Access;

[TestClass]
public sealed class AccountEndpointsTests
{
    [TestMethod]
    [DataRow("/", "/")]
    [DataRow("/settings", "/settings")]
    [DataRow("/requests?Host=example.com&p=2", "/requests?Host=example.com&p=2")]
    [DataRow(null, "/")]
    [DataRow("", "/")]
    [DataRow("settings", "/")]
    [DataRow("//evil.example", "/")]
    [DataRow("/\\evil.example", "/")]
    [DataRow("https://evil.example/", "/")]
    public void LocalUrlOrRoot_KeepsOnlyPathsOnThisSite(string? returnUrl, string expected)
    {
        Assert.AreEqual(expected, AccountEndpoints.LocalUrlOrRoot(returnUrl));
    }
}
