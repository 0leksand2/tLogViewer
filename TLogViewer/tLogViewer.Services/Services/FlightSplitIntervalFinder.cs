using tLogViewer.Core.Enums.Heartbeat;

namespace tLogViewer.Services.Services;

public static class FlightSplitIntervalFinder
{
    /// <summary>
    /// Prefers power-up (UNINIT/BOOT) boundaries; falls back to armed/disarmed when no boot markers exist.
    /// </summary>
    public static List<(DateTimeOffset From, DateTimeOffset Until)> Find(
        IEnumerable<VehicleHeartbeatSelector.HeartbeatSample> heartbeats,
        DateTimeOffset logStart,
        DateTimeOffset logEnd,
        TimeSpan margin)
    {
        var samples = heartbeats.OrderBy(static sample => sample.Time).ToList();
        if (samples.Count == 0)
        {
            return [(logStart, logEnd)];
        }

        var powerUps = PowerUpIntervalFinder.FindPowerUpEvents(
            samples.Select(static sample => (sample.Time, sample.SystemStatus)));

        if (powerUps.Count > 0)
        {
            return PowerUpIntervalFinder.BuildFlightIntervals(powerUps, logStart, logEnd, margin);
        }

        return BuildArmedIntervals(samples, logStart, logEnd, margin);
    }

    private static List<(DateTimeOffset From, DateTimeOffset Until)> BuildArmedIntervals(
        IReadOnlyList<VehicleHeartbeatSelector.HeartbeatSample> heartbeats,
        DateTimeOffset logStart,
        DateTimeOffset logEnd,
        TimeSpan margin)
    {
        var armedIntervals = ArmedIntervalFinder.Find(
            heartbeats.Select(static sample => (sample.Time, sample.Armed)));

        if (armedIntervals.Count == 0)
        {
            return [(logStart, logEnd)];
        }

        var intervals = new List<(DateTimeOffset, DateTimeOffset)>(armedIntervals.Count);
        foreach (var (armedFrom, armedUntil) in armedIntervals)
        {
            var from = Clamp(armedFrom - margin, logStart, logEnd);
            var until = Clamp(armedUntil + margin, logStart, logEnd);

            if (from > until)
            {
                (from, until) = (until, from);
            }

            intervals.Add((from, until));
        }

        return intervals;
    }

    private static DateTimeOffset Clamp(DateTimeOffset value, DateTimeOffset min, DateTimeOffset max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }
}
