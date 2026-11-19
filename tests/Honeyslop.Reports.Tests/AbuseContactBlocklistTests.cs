namespace Honeyslop.Reports.Tests;

[TestClass]
public sealed class AbuseContactBlocklistTests
{
    [TestMethod]
    public void Parse_SplitsTrimsLowersAndDropsRepeats()
    {
        CollectionAssert.AreEqual(
            new[]
            {
                "abuse@bouncing.example", "@spam.example"
            },
            AbuseContactBlocklist.Parse([" Abuse@Bouncing.example;@spam.example\n\n abuse@bouncing.example, ", "@SPAM.example"]));
    }

    [TestMethod]
    [DataRow("abuse@bouncing.example", true)]
    [DataRow(" ABUSE@bouncing.example ", true)]
    [DataRow("noc@bouncing.example", false)]
    [DataRow("abuse@spam.example", true)]
    [DataRow("abuse@mail.spam.example", false)]
    [DataRow("abuse@example.test", false)]
    public void Blocks_TakesAddressesExactlyAndDomainsWhole(string address, bool blocked)
    {
        Assert.AreEqual(blocked, AbuseContactBlocklist.Blocks(["abuse@bouncing.example", "@spam.example"], address));
    }

    [TestMethod]
    public void Candidates_AreEachAddressAndItsDomain()
    {
        CollectionAssert.AreEqual(
            new[]
            {
                "abuse@bouncing.example", "@bouncing.example", "noc@bouncing.example", "postmaster"
            },
            AbuseContactBlocklist.Candidates([" Abuse@Bouncing.example", "noc@bouncing.example", "postmaster"]));
    }

    [TestMethod]
    public void Problem_NamesTheFirstEntryThatIsNeitherAnAddressNorADomain()
    {
        Assert.IsNull(AbuseContactBlocklist.Problem(["abuse@bouncing.example", "@spam.example"]));
        StringAssert.Contains(AbuseContactBlocklist.Problem(["abuse@bouncing.example", "bouncing.example"]), "bouncing.example");
    }
}