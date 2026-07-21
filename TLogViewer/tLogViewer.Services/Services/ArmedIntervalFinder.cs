namespace tLogViewer.Services.Services;

public static class ArmedIntervalFinder
{
    public static List<(DateTimeOffset ArmedFrom, DateTimeOffset ArmedUntil)> Find(
        IEnumerable<(DateTimeOffset Time, bool Armed)> heartbeatStates)
    {
        var intervals = new List<(DateTimeOffset, DateTimeOffset)>();
        bool? wasArmed = null;
        DateTimeOffset? armedFrom = null;
        DateTimeOffset? lastTime = null;

        foreach (var (time, armed) in heartbeatStates)
        {
            lastTime = time;

            if (wasArmed != true && armed)
            {
                armedFrom = time;
            }
            else if (wasArmed == true && !armed && armedFrom.HasValue)
            {
                intervals.Add((armedFrom.Value, time));
                armedFrom = null;
            }

            wasArmed = armed;
        }

        if (armedFrom.HasValue && lastTime.HasValue)
        {
            intervals.Add((armedFrom.Value, lastTime.Value));
        }

        return intervals;
    }

    public static DateTimeOffset TrailToUtc(ulong trailUs) =>
        DateTimeOffset.UnixEpoch.AddTicks(checked((long)(trailUs * 10)));

    public static ulong UtcToTrail(DateTimeOffset utc) =>
        checked((ulong)((utc - DateTimeOffset.UnixEpoch).Ticks / 10));
}
