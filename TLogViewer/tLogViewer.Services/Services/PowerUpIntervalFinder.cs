using tLogViewer.Core.Enums.Heartbeat;

namespace tLogViewer.Services.Services;

public static class PowerUpIntervalFinder
{
    /// <summary>
    /// Detects vehicle power-up from heartbeat <c>system_status</c>:
    /// <see cref="SystemStatus.UNINIT"/> or <see cref="SystemStatus.BOOT"/>
    /// after power-off or a completed prior session.
    /// </summary>
    public static List<DateTimeOffset> FindPowerUpEvents(
        IEnumerable<(DateTimeOffset Time, SystemStatus Status)> heartbeatStatuses)
    {
        var events = new List<DateTimeOffset>();
        SystemStatus? previous = null;

        foreach (var (time, status) in heartbeatStatuses)
        {
            if (IsPowerUpTransition(previous, status))
            {
                events.Add(time);
            }

            previous = status;
        }

        return events;
    }

    public static List<(DateTimeOffset From, DateTimeOffset Until)> BuildFlightIntervals(
        IReadOnlyList<DateTimeOffset> powerUpEvents,
        DateTimeOffset logStart,
        DateTimeOffset logEnd,
        TimeSpan margin)
    {
        if (powerUpEvents.Count == 0)
        {
            return [(logStart, logEnd)];
        }

        var intervals = new List<(DateTimeOffset, DateTimeOffset)>(powerUpEvents.Count);

        for (var i = 0; i < powerUpEvents.Count; i++)
        {
            var from = Clamp(powerUpEvents[i] - margin, logStart, logEnd);
            var untilBound = i + 1 < powerUpEvents.Count
                ? powerUpEvents[i + 1] - margin
                : logEnd + margin;
            var until = Clamp(untilBound, logStart, logEnd);

            if (from > until)
            {
                (from, until) = (until, from);
            }

            intervals.Add((from, until));
        }

        return intervals;
    }

    public static bool TryParseSystemStatus(string? value, out SystemStatus status)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            status = default;
            return false;
        }

        return Enum.TryParse(value, ignoreCase: true, out status);
    }

    private static bool IsPowerUpTransition(SystemStatus? previous, SystemStatus current) =>
        current is SystemStatus.UNINIT or SystemStatus.BOOT
        && (previous is null
            or SystemStatus.POWEROFF
            or SystemStatus.ACTIVE
            or SystemStatus.STANDBY
            or SystemStatus.CRITICAL
            or SystemStatus.EMERGENCY);

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
