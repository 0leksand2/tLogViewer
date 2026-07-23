namespace tLogViewer.Services.Services;

/// <summary>
/// Fills DERIVED (998) fields on every flight millisecond:
/// link quality is forward-filled from 1 Hz samples;
/// time-since-arm is computed at each millisecond from heartbeat armed state;
/// bearing to MAV and distance to home from home → plane position.
/// </summary>
public static class DerivedFieldsEnricher
{
    private static readonly string LinkQualityKey = FlightFieldIds.LinkQualityGcs;
    private static readonly string TimeSinceArmKey = FlightFieldIds.TimeSinceArmSec;
    private static readonly string AzToMavKey = FlightFieldIds.AzToMav;
    private static readonly string DistToHomeKey = FlightFieldIds.DistToHome;
    private static readonly string ArmedKey = FlightFieldIds.Armed;
    private static readonly string HomeLatKey = FlightFieldIds.HomeLatitudeDeg;
    private static readonly string HomeLonKey = FlightFieldIds.HomeLongitudeDeg;
    private static readonly string PlaneLatKey = FlightFieldIds.AliasLat;
    private static readonly string PlaneLonKey = FlightFieldIds.AliasLon;

    private const double EarthRadiusM = 6_371_000;

    public static void ForwardFill(Dictionary<long, Dictionary<string, object>> byMillisecond)
    {
        if (byMillisecond.Count == 0)
        {
            return;
        }

        object? lastLinkQuality = null;
        bool? armed = null;
        long? armedFromMs = null;
        double? homeLat = null;
        double? homeLon = null;

        foreach (var ms in byMillisecond.Keys.OrderBy(static key => key))
        {
            var atMs = byMillisecond[ms];

            if (atMs.TryGetValue(LinkQualityKey, out var linkQuality))
            {
                lastLinkQuality = linkQuality;
            }
            else if (lastLinkQuality is not null)
            {
                atMs[LinkQualityKey] = lastLinkQuality;
            }

            if (atMs.TryGetValue(ArmedKey, out var armedObj) && armedObj is bool isArmed)
            {
                if (isArmed && armed != true)
                {
                    armedFromMs = ms;
                }
                else if (!isArmed)
                {
                    armedFromMs = null;
                }

                armed = isArmed;
            }

            double timeSinceArmSec = 0;
            if (armed == true && armedFromMs.HasValue)
            {
                timeSinceArmSec = Math.Max(0, (ms - armedFromMs.Value) / 1000.0);
            }

            atMs[TimeSinceArmKey] = timeSinceArmSec;

            if (TryReadCoordinate(atMs, HomeLatKey, HomeLonKey, out var nextHomeLat, out var nextHomeLon))
            {
                homeLat = nextHomeLat;
                homeLon = nextHomeLon;
            }

            if (homeLat is null
                || homeLon is null
                || !TryReadCoordinate(atMs, PlaneLatKey, PlaneLonKey, out var planeLat, out var planeLon))
            {
                continue;
            }

            atMs[DistToHomeKey] = HaversineDistanceM(homeLat.Value, homeLon.Value, planeLat, planeLon);
            atMs[AzToMavKey] = InitialBearingDeg(homeLat.Value, homeLon.Value, planeLat, planeLon);
        }
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

        if (!fields.TryGetValue(latKey, out var latObj)
            || !fields.TryGetValue(lonKey, out var lonObj)
            || !TryAsDouble(latObj, out latitudeDeg)
            || !TryAsDouble(lonObj, out longitudeDeg))
        {
            return false;
        }

        return !IsInvalidCoordinate(latitudeDeg, longitudeDeg);
    }

    /// <summary>Ground distance in meters between two WGS84 positions.</summary>
    private static double HaversineDistanceM(
        double lat1Deg,
        double lon1Deg,
        double lat2Deg,
        double lon2Deg)
    {
        var lat1 = lat1Deg * Math.PI / 180.0;
        var lon1 = lon1Deg * Math.PI / 180.0;
        var lat2 = lat2Deg * Math.PI / 180.0;
        var lon2 = lon2Deg * Math.PI / 180.0;
        var dLat = lat2 - lat1;
        var dLon = lon2 - lon1;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(Math.Max(0, 1 - a)));
        return EarthRadiusM * c;
    }

    /// <summary>Initial bearing from point 1 → point 2 in degrees (0–360, north = 0).</summary>
    private static double InitialBearingDeg(
        double lat1Deg,
        double lon1Deg,
        double lat2Deg,
        double lon2Deg)
    {
        var lat1 = lat1Deg * Math.PI / 180.0;
        var lat2 = lat2Deg * Math.PI / 180.0;
        var dLon = (lon2Deg - lon1Deg) * Math.PI / 180.0;

        var y = Math.Sin(dLon) * Math.Cos(lat2);
        var x = Math.Cos(lat1) * Math.Sin(lat2)
            - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
        var bearing = Math.Atan2(y, x) * 180.0 / Math.PI;
        return NormalizeHeading(bearing);
    }

    private static double NormalizeHeading(double degrees)
    {
        var normalized = degrees % 360;
        if (normalized < 0)
        {
            normalized += 360;
        }

        return normalized;
    }

    private static bool IsInvalidCoordinate(double latitudeDeg, double longitudeDeg) =>
        Math.Abs(latitudeDeg) < 1e-9 && Math.Abs(longitudeDeg) < 1e-9;

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
            case decimal m:
                result = (double)m;
                return true;
            default:
                result = 0;
                return false;
        }
    }
}
