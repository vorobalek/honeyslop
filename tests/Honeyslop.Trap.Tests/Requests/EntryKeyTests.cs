using Honeyslop.Data.Requests;

namespace Honeyslop.Trap.Tests.Requests;

[TestClass]
public sealed class EntryKeyTests
{
    [TestMethod]
    public void Create_SameRequest_ReturnsSameHash()
    {
        var first = EntryKey.Create("GET", "kms.example.test", "/.env");
        var second = EntryKey.Create("GET", "kms.example.test", "/.env");

        CollectionAssert.AreEqual(first.Hash, second.Hash);
    }

    [TestMethod]
    [DataRow("POST", "kms.example.test", "/.env")]
    [DataRow("GET", "vps.example.test", "/.env")]
    [DataRow("GET", "kms.example.test", "/.env?x=1")]
    public void Create_DifferentMethodHostOrTarget_ReturnsDifferentHash(string method, string host, string target)
    {
        var baseline = EntryKey.Create("GET", "kms.example.test", "/.env");
        var other = EntryKey.Create(method, host, target);

        CollectionAssert.AreNotEqual(baseline.Hash, other.Hash);
    }
}