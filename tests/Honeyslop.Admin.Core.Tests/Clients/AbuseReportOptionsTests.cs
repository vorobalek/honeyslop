using Honeyslop.Admin.Core.Options;

namespace Honeyslop.Admin.Core.Tests.Clients;

[TestClass]
public sealed class AbuseReportOptionsTests
{
    [TestMethod]
    [DataRow("Example Sender", "Example Sender <security@example.test>")]
    [DataRow("Example Sender, security team", "\"Example Sender, security team\" <security@example.test>")]
    [DataRow(null, "security@example.test")]
    public void ExpectedFrom_QuotesTheNameOnlyWhenItNeedsIt(string? senderName, string expected)
    {
        var options = new AbuseReportOptions { FromAddress = "security@example.test" };

        Assert.AreEqual(expected, options.ExpectedFrom(senderName));
    }

    [TestMethod]
    public void ExpectedFrom_WithoutAddress_IsUnknown()
    {
        Assert.IsNull(new AbuseReportOptions().ExpectedFrom("Example Sender"));
    }
}
