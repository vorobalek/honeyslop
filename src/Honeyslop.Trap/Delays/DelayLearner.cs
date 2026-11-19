using Honeyslop.Data.Entities;

namespace Honeyslop.Trap.Delays;

internal static class DelayLearner
{
    public const int Margin = 100;
    public const int FirstProbeEvery = 20;
    private const int MaxProbeEvery = 1 << 20;
    private const double ClimbGrowth = 1.25;
    private const double ProbeGrowth = 1.2;
    private const double SameDrop = 0.1;

    public static int Middle(int min, int max)
    {
        return min + (max - min) / 2;
    }

    public static ClientProbe Start(string clientIp, int min, int max, DateTimeOffset now)
    {
        return new ClientProbe
        {
            ClientIp = clientIp,
            DelayMilliseconds = Middle(min, max),
            ProbeEvery = FirstProbeEvery,
            UpdatedAt = now
        };
    }

    public static ClientProbe Observe(ClientProbe current, bool answered, int durationMs, int min, int max, DateTimeOffset now)
    {
        var probe = new ClientProbe
        {
            ClientIp = current.ClientIp,
            DelayMilliseconds = current.DelayMilliseconds,
            TimeoutMilliseconds = current.TimeoutMilliseconds,
            ProbeEvery = current.ProbeEvery,
            AnswersSinceProbe = current.AnswersSinceProbe,
            Probing = current.Probing,
            EarlyDropMilliseconds = current.EarlyDropMilliseconds,
            Version = current.Version,
            UpdatedAt = now
        };
        if (answered)
            return Answered(probe, durationMs, min, max);
        if (probe.TimeoutMilliseconds is { } known && Math.Abs(durationMs - known) <= SameDrop * known)
        {
            if (probe.Probing)
                probe.ProbeEvery = Math.Min(probe.ProbeEvery * 2, MaxProbeEvery);
            return Settle(probe, Math.Min(known, durationMs), min, max);
        }

        if (probe.TimeoutMilliseconds is null || durationMs > probe.DelayMilliseconds / 2)
        {
            probe.ProbeEvery = FirstProbeEvery;
            return Settle(probe, durationMs, min, max);
        }

        if (probe.EarlyDropMilliseconds is { } early && Math.Abs(durationMs - early) <= SameDrop * early)
        {
            probe.ProbeEvery = FirstProbeEvery;
            return Settle(probe, durationMs, min, max);
        }

        probe.EarlyDropMilliseconds = durationMs;
        return probe;
    }

    private static ClientProbe Answered(ClientProbe probe, int durationMs, int min, int max)
    {
        probe.EarlyDropMilliseconds = null;
        if (probe.TimeoutMilliseconds is not { } timeout || durationMs >= timeout)
        {
            probe.TimeoutMilliseconds = null;
            probe.ProbeEvery = FirstProbeEvery;
            probe.AnswersSinceProbe = 0;
            probe.Probing = false;
            probe.DelayMilliseconds = Clamp(Math.Max(probe.DelayMilliseconds, (int)Math.Min(int.MaxValue, durationMs * ClimbGrowth)), min, max);
            return probe;
        }

        probe.AnswersSinceProbe++;
        if (probe.Probing || probe.AnswersSinceProbe < probe.ProbeEvery)
            return probe;
        probe.AnswersSinceProbe = 0;
        probe.Probing = true;
        probe.DelayMilliseconds = Clamp((int)Math.Min(int.MaxValue, timeout * ProbeGrowth), min, max);
        return probe;
    }

    private static ClientProbe Settle(ClientProbe probe, int timeout, int min, int max)
    {
        probe.TimeoutMilliseconds = timeout;
        probe.EarlyDropMilliseconds = null;
        probe.AnswersSinceProbe = 0;
        probe.Probing = false;
        probe.DelayMilliseconds = Clamp(timeout - Margin, min, max);
        return probe;
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Clamp(value, min, Math.Max(min, max));
    }
}