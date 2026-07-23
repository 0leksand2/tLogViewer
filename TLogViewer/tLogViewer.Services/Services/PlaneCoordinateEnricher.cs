namespace tLogViewer.Services.Services;

/// <summary>
/// Adds plane/target/wind alias fields (997_*) to each flight millisecond.
/// Plane position from messages that change over time; yaw from VFR_HUD heading (message 74).
/// Wind direction from WIND (168). Absolute target from POSITION_TARGET_GLOBAL_INT (87); otherwise derived from
/// NAV_CONTROLLER_OUTPUT (62) target bearing + waypoint distance.
/// </summary>
public static class PlaneCoordinateEnricher
{
    private static readonly string VfrHudHeadingKey = FlightFieldIds.VfrHeadingDeg;
    private static readonly string VfrHudClimbKey = FlightFieldIds.VfrClimbMS;
    private static readonly string PositionTargetLatKey = FlightFieldIds.PositionTargetLat;
    private static readonly string PositionTargetLonKey = FlightFieldIds.PositionTargetLon;
    private static readonly string PositionTargetYawKey = FlightFieldIds.PositionTargetYaw;
    private static readonly string PositionTargetAltKey = FlightFieldIds.PositionTargetAlt;
    private static readonly string NavTargetBearingKey = FlightFieldIds.NavTargetBearing;
    private static readonly string NavBearingKey = FlightFieldIds.NavBearing;
    private static readonly string NavWpDistKey = FlightFieldIds.NavWpDist;
    private static readonly string WindDirectionKey = FlightFieldIds.WindDirection;
    private static readonly string WindSpeedKey = FlightFieldIds.WindSpeed;
    private const double EarthRadiusM = 6_371_000;

    /// <summary>Candidate lat/lon key pairs, preferred order.</summary>
    private static readonly (string LatKey, string LonKey, string MessageId, int Priority)[] LatLonSources =
    [
        (FlightFieldIds.GlobalPosLat, FlightFieldIds.GlobalPosLon, "33", 0),
        (FlightFieldIds.GpsRawLat, FlightFieldIds.GpsRawLon, "24", 1),
        (FlightFieldIds.PositionTargetLat, FlightFieldIds.PositionTargetLon, "87", 2),
        (FlightFieldIds.Format("73", "latitudeDeg"), FlightFieldIds.Format("73", "longitudeDeg"), "73", 3),
    ];

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
        var orderedMs = byMillisecond.Keys.OrderBy(static ms => ms).ToList();
        double? lastLat = null;
        double? lastLon = null;
        double? lastYaw = null;
        double? lastVerticalSpeed = null;
        double? lastTargetLat = null;
        double? lastTargetLon = null;
        double? lastTargetYaw = null;
        double? lastTargetAlt = null;
        double? lastNavTargetBearing = null;
        double? lastNavBearing = null;
        double? lastNavWpDistM = null;
        double? lastWindDir = null;
        double? lastWindSpeed = null;
        var hasAbsoluteTarget = false;

        string? latKey = null;
        string? lonKey = null;
        if (source is not null)
        {
            (latKey, lonKey) = source.Value;
        }

        foreach (var ms in orderedMs)
        {
            var atMs = byMillisecond[ms];

            if (latKey is not null
                && lonKey is not null
                && TryReadCoordinate(atMs, latKey, lonKey, out var lat, out var lon))
            {
                lastLat = lat;
                lastLon = lon;
            }

            if (atMs.TryGetValue(VfrHudHeadingKey, out var yawObj) && TryAsDouble(yawObj, out var yaw))
            {
                lastYaw = NormalizeHeading(yaw);
            }

            if (atMs.TryGetValue(VfrHudClimbKey, out var climbObj) && TryAsDouble(climbObj, out var climb))
            {
                lastVerticalSpeed = climb;
            }

            if (TryReadCoordinate(atMs, PositionTargetLatKey, PositionTargetLonKey, out var targetLat, out var targetLon))
            {
                lastTargetLat = targetLat;
                lastTargetLon = targetLon;
                hasAbsoluteTarget = true;
            }

            if (atMs.TryGetValue(PositionTargetYawKey, out var targetYawObj)
                && TryAsDouble(targetYawObj, out var targetYaw))
            {
                lastTargetYaw = NormalizeHeading(targetYaw);
            }

            if (atMs.TryGetValue(PositionTargetAltKey, out var targetAltObj)
                && TryAsDouble(targetAltObj, out var targetAlt))
            {
                lastTargetAlt = targetAlt;
            }

            if (atMs.TryGetValue(NavTargetBearingKey, out var bearingObj)
                && TryAsDouble(bearingObj, out var bearing))
            {
                lastNavTargetBearing = NormalizeHeading(bearing);
            }

            if (atMs.TryGetValue(NavBearingKey, out var navBearingObj)
                && TryAsDouble(navBearingObj, out var navBearing))
            {
                lastNavBearing = NormalizeHeading(navBearing);
            }

            if (atMs.TryGetValue(NavWpDistKey, out var distObj)
                && TryAsDouble(distObj, out var dist)
                && dist >= 0)
            {
                lastNavWpDistM = dist;
            }

            if (atMs.TryGetValue(WindDirectionKey, out var windDirObj)
                && TryAsDouble(windDirObj, out var windDir))
            {
                lastWindDir = NormalizeHeading(windDir);
            }

            if (atMs.TryGetValue(WindSpeedKey, out var windSpeedObj)
                && TryAsDouble(windSpeedObj, out var windSpeed)
                && windSpeed >= 0)
            {
                lastWindSpeed = windSpeed;
            }

            if (!hasAbsoluteTarget
                && lastLat.HasValue
                && lastLon.HasValue
                && lastNavTargetBearing.HasValue
                && lastNavWpDistM is > 0.5)
            {
                var (navLat, navLon) = DestinationPoint(
                    lastLat.Value,
                    lastLon.Value,
                    lastNavTargetBearing.Value,
                    lastNavWpDistM.Value);
                lastTargetLat = navLat;
                lastTargetLon = navLon;
                lastTargetYaw = lastNavTargetBearing;
            }

            if (lastLat.HasValue && lastLon.HasValue)
            {
                atMs[FlightFieldIds.AliasLat] = lastLat.Value;
                atMs[FlightFieldIds.AliasLon] = lastLon.Value;
            }

            if (lastYaw.HasValue)
            {
                atMs[FlightFieldIds.AliasYaw] = lastYaw.Value;
            }

            if (lastVerticalSpeed.HasValue)
            {
                atMs[FlightFieldIds.VerticalSpeed] = lastVerticalSpeed.Value;
            }

            if (lastTargetLat.HasValue && lastTargetLon.HasValue)
            {
                atMs[FlightFieldIds.AliasTargetLat] = lastTargetLat.Value;
                atMs[FlightFieldIds.AliasTargetLon] = lastTargetLon.Value;
            }

            if (lastTargetYaw.HasValue)
            {
                atMs[FlightFieldIds.AliasTargetYaw] = lastTargetYaw.Value;
            }

            if (lastTargetAlt.HasValue)
            {
                atMs[FlightFieldIds.AliasTargetAlt] = lastTargetAlt.Value;
            }

            if (lastNavTargetBearing.HasValue)
            {
                atMs[FlightFieldIds.AliasTargetBearing] = lastNavTargetBearing.Value;
            }

            if (lastNavBearing.HasValue)
            {
                atMs[FlightFieldIds.AliasNavBearing] = lastNavBearing.Value;
            }

            if (lastNavWpDistM.HasValue)
            {
                atMs[FlightFieldIds.AliasWpDistM] = lastNavWpDistM.Value;
            }

            if (lastWindDir.HasValue)
            {
                atMs[FlightFieldIds.AliasWindDir] = lastWindDir.Value;
            }

            if (lastWindSpeed.HasValue)
            {
                atMs[FlightFieldIds.AliasWindSpeed] = lastWindSpeed.Value;
            }
        }
    }

    private static (string LatKey, string LonKey)? SelectChangingSource(
        Dictionary<long, Dictionary<string, object>> byMillisecond)
    {
        var candidates = LatLonSources
            .Where(static source => !ExcludedMessageIds.Contains(source.MessageId))
            .Select(source => (
                LatKey: source.LatKey,
                LonKey: source.LonKey,
                Priority: source.Priority,
                Distinct: CountDistinctPositions(byMillisecond, source.LatKey, source.LonKey)))
            .Where(static item => item.Distinct > 1)
            .OrderBy(static item => item.Priority)
            .ThenByDescending(static item => item.Distinct)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        var best = candidates[0];
        return (best.LatKey, best.LonKey);
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
            case double d when double.IsFinite(d):
                result = d;
                return true;
            case float f when float.IsFinite(f):
                result = f;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case short s:
                result = s;
                return true;
            case ushort us:
                result = us;
                return true;
            case byte b:
                result = b;
                return true;
            case decimal m:
                result = (double)m;
                return true;
            default:
                result = 0;
                return false;
        }
    }

    private static (double LatitudeDeg, double LongitudeDeg) DestinationPoint(
        double latitudeDeg,
        double longitudeDeg,
        double bearingDeg,
        double distanceM)
    {
        var lat1 = latitudeDeg * Math.PI / 180.0;
        var lon1 = longitudeDeg * Math.PI / 180.0;
        var bearing = bearingDeg * Math.PI / 180.0;
        var angularDistance = distanceM / EarthRadiusM;

        var lat2 = Math.Asin(
            Math.Sin(lat1) * Math.Cos(angularDistance)
            + Math.Cos(lat1) * Math.Sin(angularDistance) * Math.Cos(bearing));
        var lon2 = lon1 + Math.Atan2(
            Math.Sin(bearing) * Math.Sin(angularDistance) * Math.Cos(lat1),
            Math.Cos(angularDistance) - Math.Sin(lat1) * Math.Sin(lat2));

        return (lat2 * 180.0 / Math.PI, lon2 * 180.0 / Math.PI);
    }

    private static long Quantize(double degrees) => (long)Math.Round(degrees * 1e7);

    private static bool IsInvalidCoordinate(double latitudeDeg, double longitudeDeg) =>
        Math.Abs(latitudeDeg) < 1e-9 && Math.Abs(longitudeDeg) < 1e-9;

    private static double NormalizeHeading(double degrees)
    {
        var normalized = degrees % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }
}
