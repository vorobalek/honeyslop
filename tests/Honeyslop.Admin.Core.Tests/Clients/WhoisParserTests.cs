using System.Text.Json;
using Honeyslop.Admin.Core.Clients.Internals;
using Honeyslop.Admin.Core.Contracts;

namespace Honeyslop.Admin.Core.Tests.Clients;

[TestClass]
public sealed class WhoisParserTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Parse_ArinNetwork_TakesPrefixOriginNetworkAndAbuseContact()
    {
        var info = Parse(RipeStatFixtures.PrefixOverview, RipeStatFixtures.AbuseContactFinder, RipeStatFixtures.Whois);

        Assert.AreEqual("8.8.8.0/24", info.Prefix);
        CollectionAssert.AreEqual(new[] { new WhoisOriginDto(15169, "GOOGLE - Google LLC") }, info.Origins);
        Assert.AreEqual("8.8.8.0/24", info.Network);
        Assert.AreEqual("GOGL", info.NetworkName);
        Assert.AreEqual("Google LLC (GOGL)", info.Owner);
        Assert.AreEqual("US", info.Country);
        Assert.AreEqual("ARIN", info.Registry);
        CollectionAssert.AreEqual(new[] { "network-abuse@google.com" }, info.AbuseContacts);
        Assert.Contains("NetName: GOGL", info.Records);
        Assert.Contains("OrgAbuseEmail: network-abuse@google.com", info.Records);
        Assert.AreEqual(Now, info.LookedUpAt);
    }

    [TestMethod]
    public void Parse_UnannouncedAddressWithoutRecords_LeavesEverythingEmpty()
    {
        var info = Parse(RipeStatFixtures.NotAnnounced, RipeStatFixtures.NoAbuseContacts, """{"data": {"records": []}}""");

        Assert.IsNull(info.Prefix);
        Assert.IsEmpty(info.Origins);
        Assert.IsNull(info.Network);
        Assert.IsNull(info.Owner);
        Assert.IsNull(info.Registry);
        Assert.IsEmpty(info.AbuseContacts);
        Assert.AreEqual(string.Empty, info.Records);
    }

    [TestMethod]
    public void Parse_RipeNetwork_ReadsLowerCaseKeysAndFallsBackToTheWhoisAuthority()
    {
        var info = Parse(RipeStatFixtures.NotAnnounced, RipeStatFixtures.NoAbuseContacts, RipeStatFixtures.RipeWhois);

        Assert.AreEqual("193.0.0.0/21", info.Network);
        Assert.AreEqual("RIPE-NCC", info.NetworkName);
        Assert.AreEqual("RIPE Network Coordination Centre", info.Owner);
        Assert.AreEqual("NL", info.Country);
        Assert.AreEqual("RIPE NCC", info.Registry);
    }

    private static WhoisDto Parse(string prefixOverview, string abuseContactFinder, string whois)
    {
        return WhoisParser.Parse(Data(prefixOverview), Data(abuseContactFinder), Data(whois), Now);
    }

    private static JsonElement Data(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("data").Clone();
    }
}
