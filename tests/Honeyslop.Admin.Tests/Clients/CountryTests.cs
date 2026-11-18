using Honeyslop.Admin.Clients;
using Microsoft.Extensions.Logging.Abstractions;

namespace Honeyslop.Admin.Tests.Clients;

[TestClass]
public sealed class CountryTests
{
    [TestMethod]
    [DataRow("US", "🇺🇸")]
    [DataRow("NL", "🇳🇱")]
    [DataRow("DE", "🇩🇪")]
    public void Flag_IsTheRegionalIndicatorPair(string code, string flag)
    {
        Assert.AreEqual(flag, new Country(code, code).Flag);
    }

    [TestMethod]
    public void Find_WithoutDatabase_ReturnsNothing()
    {
        using var lookup = new CountryLookup(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), NullLogger<CountryLookup>.Instance);

        Assert.IsNull(lookup.Find("8.8.8.8"));
    }
}
