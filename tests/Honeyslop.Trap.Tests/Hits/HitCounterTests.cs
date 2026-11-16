using Honeyslop.Trap.Hits;

namespace Honeyslop.Trap.Tests.Hits;

[TestClass]
public sealed class HitCounterTests
{
    [TestMethod]
    public void Drain_ReturnsAggregatedHitsAndEmptiesTheCounter()
    {
        var counter = new HitCounter();
        var first = DateTimeOffset.UtcNow;
        var latest = first.AddSeconds(5);

        counter.Add(1, latest);
        counter.Add(1, first);
        counter.Add(2, first);

        var drained = counter.Drain().OrderBy(x => x.EntryId).ToArray();
        CollectionAssert.AreEqual(
            new[] { new HitBatch(1, 2, latest), new HitBatch(2, 1, first) },
            drained);
        Assert.AreEqual(0, counter.Drain().Count);
    }
}
