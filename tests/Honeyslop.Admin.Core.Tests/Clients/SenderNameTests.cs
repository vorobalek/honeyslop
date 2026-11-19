using Honeyslop.Admin.Core.Text;

namespace Honeyslop.Admin.Core.Tests.Clients;

[TestClass]
public sealed class SenderNameTests
{
    [TestMethod]
    [DataRow("  Example   Researcher,\tsecurity team ", "Example Researcher, security team")]
    [DataRow("Example\r\nBcc: someone@example.test", "Example Bcc: someone@example.test")]
    [DataRow("   ", null)]
    [DataRow(null, null)]
    public void Clean_FoldsWhitespaceAndControlCharacters(string? value, string? expected)
    {
        Assert.AreEqual(expected, SenderName.Clean(value));
    }

    [TestMethod]
    public void Clean_CutsLongNames()
    {
        Assert.HasCount(128, SenderName.Clean(new string('a', 300))!);
    }
}