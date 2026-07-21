namespace tLogViewer.Services.Services;

/// <summary>
/// Adds <c>lat</c> / <c>lon</c> and <c>yaw</c> to each flight millisecond.
/// Position from messages that change over time; yaw from VFR_HUD heading (message 74).
/// </summary>
public static class PlaneCoordinateEnricher
{
    private const string LatitudeSuffix = "_latitudeDeg";
    private const string LongitudeSuffix = "_longitudeDeg";
    private const string VfrHudHeadingKey = "74_headingDeg";

    /// <summary>Lower = preferred vehicle position source.</summary>
    private static readonly Dictionary<string, int> SourcePriority = new(StringComparer.Ordinal)
    {
        ["33"] = 0,  // GLOBAL_POSITION_INT
        ["24"] = 1,  // GPS_RAW_INT
        ["87"] = 2,  // POSITION_TARGET_GLOBAL_INT
        ["73"] = 3,  // MISSION_ITEM_INT
    };

    /// <summary>Home / EKF origin — excluded from plane track selection.</summary>
    private static readonly HashSet<string> ExcludedMessageIds = new(StringComparer.Ordinal)
    {
        "242", // HOME_POSITION
        "49",  // GPS_GLOBAL_ORIGIN
    };

    public static void Enrich(Dictionary<long, Dictionary<string, object>> byMillisecond)
    {
        if (byMillisecond.Count == 0)
        {
            return;
        }

        var source = SelectChangingSource(byMillisecond);
        if (source is null)
        {
            return;
        }

        var (latKey, lonKey) = source.Value;
        var orderedMs = byMillisecond.Keys.OrderBy(static ms => ms).ToList();
        double? lastLat = null;
        double? lastLon = null;
        double? lastYaw = null;

        foreach (var ms in orderedMs)
        {
            var atMs = byMillisecond[ms];

            if (TryReadCoordinate(atMs, latKey, lonKey, out var lat, out var lon))
            {
                lastLat = lat;
                lastLon = lon;
            }

            if (atMs.TryGetValue(VfrHudHeadingKey, out var yawObj) && TryAsDouble(yawObj, out var yaw))
            {
                lastYaw = NormalizeHeading(yaw);
            }

            if (lastLat.HasValue && lastLon.HasValue)
            {
                atMs["lat"] = lastLat.Value;
                atMs["lon"] = lastLon.Value;
            }

            if (lastYaw.HasValue)
            {
                atMs["yaw"] = lastYaw.Value;
            }
        }
    }

    private static (string LatKey, string LonKey)? SelectChangingSource(
        Dictionary<long, Dictionary<string, object>> byMillisecond)
    {
        var candidates = DiscoverLatLonPairs(byMillisecond)
            .Where(pair => !ExcludedMessageIds.Contains(GetMessageId(pair.LatKey)))
            .Select(pair => (Pair: pair, Distinct: CountDistinctPositions(byMillisecond, pair.LatKey, pair.LonKey)))
            .Where(static item => item.Distinct > 1)
            .OrderBy(item => GetPriority(GetMessageId(item.Pair.LatKey)))
            .ThenByDescending(static item => item.Distinct)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        var best = candidates[0].Pair;
        return (best.LatKey, best.LonKey);
    }

    private static IEnumerable<(string LatKey, string LonKey)> DiscoverLatLonPairs(
        Dictionary<long, Dictionary<string, object>> byMillisecond)
    {
        var latKeys = byMillisecond.Values
            .SelectMany(static fields => fields.Keys)
            .Where(static key => key.EndsWith(LatitudeSuffix, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal);

        foreach (var latKey in latKeys)
        {
            var messageId = GetMessageId(latKey);
            var lonKey = $"{messageId}{LongitudeSuffix}";
            if (byMillisecond.Values.Any(fields => fields.ContainsKey(lonKey)))
            {
                yield return (latKey, lonKey);
            }
        }
    }

    private static int CountDistinctPositions(
        Dictionary<long, Dictionary<string, object>> byMillisecond,
        string latKey,
        string lonKey)
    {
        var distinct = new HashSet<(long Lat, long Lng)>();

        foreach (var fields in byMillisecond.Values)
        {
            if (TryReadCoordinate(fields, latKey, lonKey, out var lat, out var lon))
            {
                distinct.Add((Quantize(lat), Quantize(lon)));
            }
        }

        return distinct.Count;
    }

    private static bool TryReadCoordinate(
        IReadOnlyDictionary<string, object> fields,
        string latKey,
        string lonKey,
        out double latitudeDeg,
        out double longitudeDeg)
    {
        latitudeDeg = 0;
        longitudeDeg = 0;

        if (!fields.TryGetValue(latKey, out var latObj) || !fields.TryGetValue(lonKey, out var lonObj))
        {
            return false;
        }

        if (!TryAsDouble(latObj, out latitudeDeg) || !TryAsDouble(lonObj, out longitudeDeg))
        {
            return false;
        }

        if (IsInvalidCoordinate(latitudeDeg, longitudeDeg))
        {
            return false;
        }

        return true;
    }

    private static bool TryAsDouble(object value, out double result)
    {
        switch (value)
        {
            case double d:
                result = d;
                return true;
            case float f:
                result = f;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case decimal m:
                result = (double)m;
                return true;
            default:
                result = 0;
                return false;
        }
    }

    private static string GetMessageId(string latKey) =>
        latKey[..^LatitudeSuffix.Length];

    private static int GetPriority(string messageId) =>
        SourcePriority.TryGetValue(messageId, out var priority) ? priority : 100;

    private static long Quantize(double degrees) => (long)Math.Round(degrees * 1e7);

    private static bool IsInvalidCoordinate(double latitudeDeg, double longitudeDeg) =>
        Math.Abs(latitudeDeg) < 1e-9 && Math.Abs(longitudeDeg) < 1e-9;

    private static double NormalizeHeading(double degrees)
    {
        var normalized = degrees % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }
}
