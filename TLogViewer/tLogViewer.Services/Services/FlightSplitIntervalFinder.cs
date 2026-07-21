using tLogViewer.Core.Enums.Heartbeat;

namespace tLogViewer.Services.Services;

public static class FlightSplitIntervalFinder
{
    /// <summary>
    /// Prefers power-up (UNINIT/BOOT) boundaries; falls back to armed/disarmed when no boot markers exist.
    /// Each interval start is snapped to the related <c>HOME_POSITION</c> message when available.
    /// </summary>
    public static List<(DateTimeOffset From, DateTimeOffset Until)> Find(
        IEnumerable<VehicleHeartbeatSelector.HeartbeatSample> heartbeats,
        IEnumerable<DateTimeOffset> homePositionTimes,
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

        var intervals = powerUps.Count > 0
            ? PowerUpIntervalFinder.BuildFlightIntervals(powerUps, logStart, logEnd, margin)
            : BuildArmedIntervals(samples, logStart, logEnd, margin);

        return SnapStartsToHomePosition(intervals, homePositionTimes);
    }

    /// <summary>
    /// Moves each flight start to the latest <c>HOME_POSITION</c> at or before the provisional start
    /// (after the previous flight ends), otherwise the first home inside the interval.
    /// </summary>
    public static List<(DateTimeOffset From, DateTimeOffset Until)> SnapStartsToHomePosition(
        IReadOnlyList<(DateTimeOffset From, DateTimeOffset Until)> intervals,
        IEnumerable<DateTimeOffset> homePositionTimes)
    {
        var homes = homePositionTimes.OrderBy(static time => time).ToList();
        if (intervals.Count == 0 || homes.Count == 0)
        {
            return intervals.ToList();
        }

        var result = new List<(DateTimeOffset From, DateTimeOffset Until)>(intervals.Count);
        var previousUntil = DateTimeOffset.MinValue;

        foreach (var (from, until) in intervals)
        {
            var homeStart = FindHomeStart(homes, previousUntil, from, until);
            var snappedFrom = homeStart ?? from;
            if (snappedFrom > until)
            {
                snappedFrom = from;
            }

            result.Add((snappedFrom, until));
            previousUntil = until;
        }

        return result;
    }

    private static DateTimeOffset? FindHomeStart(
        IReadOnlyList<DateTimeOffset> homes,
        DateTimeOffset previousUntil,
        DateTimeOffset from,
        DateTimeOffset until)
    {
        DateTimeOffset? latestBeforeOrAtStart = null;
        foreach (var home in homes)
        {
            if (home <= previousUntil)
            {
                continue;
            }

            if (home > from)
            {
                break;
            }

            latestBeforeOrAtStart = home;
        }

        if (latestBeforeOrAtStart.HasValue)
        {
            return latestBeforeOrAtStart;
        }

        foreach (var home in homes)
        {
            if (home < from)
            {
                continue;
            }

            if (home > until)
            {
                break;
            }

            return home;
        }

        return null;
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
