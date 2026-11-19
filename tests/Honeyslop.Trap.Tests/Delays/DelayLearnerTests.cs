using Honeyslop.Data.Entities;
using Honeyslop.Trap.Delays;

namespace Honeyslop.Trap.Tests.Delays;

[TestClass]
public sealed class DelayLearnerTests
{
    private const int Min = 0;
    private const int Max = 30_000;
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 0, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Start_HoldsANewClientForTheMiddleOfTheRange()
    {
        Assert.AreEqual(15_000, DelayLearner.Start("198.51.100.1", Min, Max, Now).DelayMilliseconds);
        Assert.AreEqual(7_500, DelayLearner.Start("198.51.100.1", 5_000, 10_000, Now).DelayMilliseconds);
    }

    [TestMethod]
    public void Observe_FirstDrop_TakesItsTimeAsTheTimeout()
    {
        var probe = DelayLearner.Observe(DelayLearner.Start("198.51.100.1", Min, Max, Now), false, 9_999, Min, Max, Now);

        Assert.AreEqual((9_899, (int?)9_999), (probe.DelayMilliseconds, probe.TimeoutMilliseconds));
    }

    [TestMethod]
    public void Observe_AClientThatNeverDrops_ClimbsToTheMaximum()
    {
        var probe = DelayLearner.Start("198.51.100.1", Min, Max, Now);
        for (var request = 0; request < 10; request++)
            probe = DelayLearner.Observe(probe, true, probe.DelayMilliseconds, Min, Max, Now);

        Assert.AreEqual(Max, probe.DelayMilliseconds);
        Assert.IsNull(probe.TimeoutMilliseconds);
    }

    [TestMethod]
    public void Replay_FixedTimeout_SettlesJustBelowIt_AndProbesUpLessAndLessOften()
    {
        var (probe, drops) = Replay(DelayLearner.Start("198.51.100.1", Min, Max, Now), 10_000, 5_000);

        Assert.AreEqual(9_900, probe.DelayMilliseconds);
        Assert.IsLessThanOrEqualTo(10, drops);
        Assert.IsGreaterThanOrEqualTo(1_024, probe.ProbeEvery);
    }

    [TestMethod]
    public void Replay_TimeoutFallsOrGrows_FollowsIt()
    {
        var (settled, _) = Replay(DelayLearner.Start("198.51.100.1", Min, Max, Now), 10_000, 2_000);

        var (lower, lowerDrops) = Replay(settled, 6_000, 50);
        var (higher, _) = Replay(settled, 20_000, 2_000);

        Assert.AreEqual(5_900, lower.DelayMilliseconds);
        Assert.IsLessThanOrEqualTo(3, lowerDrops);
        Assert.AreEqual(19_900, higher.DelayMilliseconds);
    }

    [TestMethod]
    public void Observe_OneEarlyDrop_IsNoise_TwoAlikeMeanTheTimeoutFell()
    {
        var settled = DelayLearner.Observe(DelayLearner.Start("198.51.100.1", Min, Max, Now), false, 10_000, Min, Max, Now);

        var noise = DelayLearner.Observe(settled, false, 1_500, Min, Max, Now);
        var answeredAfterNoise = DelayLearner.Observe(noise, true, 9_900, Min, Max, Now);
        var again = DelayLearner.Observe(noise, false, 1_550, Min, Max, Now);

        Assert.AreEqual(9_900, noise.DelayMilliseconds);
        Assert.IsNull(answeredAfterNoise.EarlyDropMilliseconds);
        Assert.AreEqual((1_450, (int?)1_550), (again.DelayMilliseconds, again.TimeoutMilliseconds));
    }

    [TestMethod]
    public void Observe_StaysWithinTheRange()
    {
        var below = DelayLearner.Observe(DelayLearner.Start("198.51.100.1", 5_000, 30_000, Now), false, 3_000, 5_000, 30_000, Now);
        var above = DelayLearner.Observe(DelayLearner.Start("198.51.100.1", 0, 8_000, Now), true, 7_000, 0, 8_000, Now);

        Assert.AreEqual(5_000, below.DelayMilliseconds);
        Assert.AreEqual(8_000, above.DelayMilliseconds);
    }

    private static (ClientProbe Probe, int Drops) Replay(ClientProbe probe, int timeout, int requests)
    {
        var drops = 0;
        for (var request = 0; request < requests; request++)
        {
            var dropped = probe.DelayMilliseconds >= timeout;
            drops += dropped ? 1 : 0;
            probe = DelayLearner.Observe(probe, !dropped, dropped ? timeout : probe.DelayMilliseconds, Min, Max, Now);
        }

        return (probe, drops);
    }
}