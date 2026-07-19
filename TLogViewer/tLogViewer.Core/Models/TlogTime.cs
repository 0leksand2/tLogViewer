using System.Globalization;

namespace tLogViewer.Core.Models;

public static class TlogTime
{
    /// <summary>
    /// Formats a TLog trail timestamp (microseconds since Unix epoch) as UTC ISO-8601.
    /// </summary>
    public static string ToUtcIso(ulong timeUs)
    {
        // 1 µs = 10 ticks (1 tick = 100 ns)
        var utc = DateTimeOffset.UnixEpoch.AddTicks(checked((long)(timeUs * 10)));
        return utc.UtcDateTime.ToString("o", CultureInfo.InvariantCulture);
    }

    public static DateTimeOffset ParseUtc(string utcIso) =>
        DateTimeOffset.Parse(
            utcIso,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeUniversal);
}
