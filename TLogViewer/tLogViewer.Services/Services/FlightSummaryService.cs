using tLogViewer.Core.Models;

namespace tLogViewer.Services.Services;

/// <summary>
/// Analyzes flight timeline fields for GPS presence (by sat count), HDOP health,
/// magnetometer anomalies, and spoof jumps (&gt; 5 km).
/// </summary>
public static class FlightSummaryService
{
    private const double SpoofJumpThresholdM = 5_000;
    private const double EarthRadiusM = 6_371_000;
    /// <summary>Eph UINT16_MAX / 100 — treat as unknown.</summary>
    private const double MaxValidHdop = 600;
    private const double MagJumpThreshold = 150;
    private const int MagJumpWindowSize = 2;
    private const double MagThrottleCorrelationThreshold = 0.5;
    private const int MagThrottleMinSamples = 30;
    private const int YawErrorMinSamples = 20;
    private const double YawErrorGrowthDeltaDeg = 5.0;
    private const int YawCogMinSamples = 10;

    private static readonly string GpsRawLatKey = FlightFieldIds.GpsRawLat;
    private static readonly string GpsRawLonKey = FlightFieldIds.GpsRawLon;
    private static readonly string GlobalPosLatKey = FlightFieldIds.GlobalPosLat;
    private static readonly string GlobalPosLonKey = FlightFieldIds.GlobalPosLon;
    private static readonly string GpsHdopKey = FlightFieldIds.GpsHdop;
    private static readonly string AliasLatKey = FlightFieldIds.AliasLat;
    private static readonly string AliasLonKey = FlightFieldIds.AliasLon;
    private static readonly string SatCountKey = "24_009";
    private static readonly string SatCount2Key = FlightFieldIds.Satcount2;
    private static readonly string GpsInputSatCountKey = "232_006";
    private static readonly string MagXKey = FlightFieldIds.Mx;
    private static readonly string MagYKey = FlightFieldIds.My;
    private static readonly string MagZKey = FlightFieldIds.Mz;
    private static readonly string MagFieldKey = FlightFieldIds.MagField;
    private static readonly string ThrottleCh3Key = FlightFieldIds.Ch3in;
    private static readonly string ThrottlePercentKey = "74_006";
    private static readonly string ErrorYawKey = FlightFieldIds.BerError;
    private static readonly string AttitudeYawKey = "30_006";
    private static readonly string GpsCogKey = FlightFieldIds.Groundcourse;

    private static readonly (string Name, string Key)[] MagFields =
    [
        ("MagX", MagXKey),
        ("MagY", MagYKey),
        ("MagZ", MagZKey),
        ("MagField", MagFieldKey),
    ];

    public static FlightSummaryReport Analyze(IReadOnlyDictionary<long, Dictionary<string, object>> byMillisecond)
    {
        var maxSatCount = 0;
        var hdopSum = 0.0;
        var hdopCount = 0;
        double? hdopMin = null;
        double? hdopMax = null;
        var spoofEvents = new List<FlightSpoofEvent>();
        var magRadiationEvents = new List<FlightMagRadiationEvent>();

        double? lastLat = null;
        double? lastLon = null;
        double? lastKnownLat = null;
        double? lastKnownLon = null;

        var magWindows = MagFields.ToDictionary(
            static f => f.Name,
            static _ => new Queue<(long Ms, double Value)>());
        var magJumpCooldown = MagFields.ToDictionary(static f => f.Name, static _ => 0);

        var magFieldSamples = new List<double>();
        var throttleSamples = new List<double>();
        var yawErrorSamples = new List<double>();
        var yawCogDiffSamples = new List<double>();

        foreach (var ms in byMillisecond.Keys.OrderBy(static key => key))
        {
            var atMs = byMillisecond[ms];

            var satCount = ReadSatCount(atMs);
            if (satCount > maxSatCount)
            {
                maxSatCount = satCount;
            }

            if (TryAsDouble(atMs, GpsHdopKey, out var hdop)
                && hdop >= 0
                && hdop < MaxValidHdop
                && double.IsFinite(hdop))
            {
                hdopSum += hdop;
                hdopCount++;
                hdopMin = hdopMin is null ? hdop : Math.Min(hdopMin.Value, hdop);
                hdopMax = hdopMax is null ? hdop : Math.Max(hdopMax.Value, hdop);
            }

            if (TryAsDouble(atMs, ErrorYawKey, out var errorYaw))
            {
                yawErrorSamples.Add(AbsoluteHeadingErrorDeg(errorYaw));
            }

            if (TryAsDouble(atMs, AttitudeYawKey, out var attitudeYaw)
                && TryAsDouble(atMs, GpsCogKey, out var gpsCog)
                && attitudeYaw != 0
                && gpsCog != 0
                && IsPlausibleHeading(attitudeYaw)
                && IsPlausibleHeading(gpsCog))
            {
                yawCogDiffSamples.Add(AbsoluteHeadingDeltaDeg(attitudeYaw, gpsCog));
            }

            if (TryReadPlaneCoordinate(atMs, out var lat, out var lon))
            {
                if (lastLat is { } prevLat && lastLon is { } prevLon)
                {
                    var distanceM = HaversineDistanceM(prevLat, prevLon, lat, lon);
                    if (distanceM > SpoofJumpThresholdM)
                    {
                        spoofEvents.Add(new FlightSpoofEvent
                        {
                            TimestampMs = ms,
                            TimestampUtc = FormatUtc(ms),
                            FromLatitudeDeg = prevLat,
                            FromLongitudeDeg = prevLon,
                            ToLatitudeDeg = lat,
                            ToLongitudeDeg = lon,
                            DistanceM = distanceM,
                        });
                    }
                }

                lastLat = lat;
                lastLon = lon;
                lastKnownLat = lat;
                lastKnownLon = lon;
            }

            foreach (var (name, key) in MagFields)
            {
                if (!TryAsDouble(atMs, key, out var magValue))
                {
                    continue;
                }

                var window = magWindows[name];
                window.Enqueue((ms, magValue));
                while (window.Count > MagJumpWindowSize)
                {
                    window.Dequeue();
                }

                if (window.Count < MagJumpWindowSize)
                {
                    continue;
                }

                var samples = window.ToArray();
                var jump = Math.Abs(samples[1].Value - samples[0].Value);
                if (jump <= MagJumpThreshold)
                {
                    continue;
                }

                // Avoid flooding: skip the next sample after a hit.
                if (magJumpCooldown[name] > 0)
                {
                    magJumpCooldown[name]--;
                    continue;
                }

                magJumpCooldown[name] = 1;
                magRadiationEvents.Add(new FlightMagRadiationEvent
                {
                    TimestampMs = samples[1].Ms,
                    TimestampUtc = FormatUtc(samples[1].Ms),
                    FieldName = name,
                    JumpPoints = jump,
                    LatitudeDeg = lastKnownLat,
                    LongitudeDeg = lastKnownLon,
                });
            }

            if (TryAsDouble(atMs, MagFieldKey, out var magField)
                && TryReadThrottle(atMs, out var throttle))
            {
                magFieldSamples.Add(magField);
                throttleSamples.Add(throttle);
            }
        }

        var gpsExists = maxSatCount > 0;
        double? hdopAvg = hdopCount > 0 ? hdopSum / hdopCount : null;
        var (health, healthLabel) = ClassifyHdop(hdopAvg);
        var magThrottleCorr = TryPearsonCorrelation(magFieldSamples, throttleSamples);
        var moveMagAway = magThrottleCorr is >= MagThrottleCorrelationThreshold
            && magFieldSamples.Count >= MagThrottleMinSamples;
        var yawErrorGrowing = IsGrowingThroughFlight(yawErrorSamples, YawErrorMinSamples, YawErrorGrowthDeltaDeg);
        double? yawErrorAvg = yawErrorSamples.Count > 0 ? yawErrorSamples.Average() : null;
        double? yawCogDiffAvg = yawCogDiffSamples.Count > 0 ? yawCogDiffSamples.Average() : null;
        var (yawCogHealth, yawCogLabel) = ClassifyYawCog(yawCogDiffAvg, yawCogDiffSamples.Count);

        return new FlightSummaryReport
        {
            GpsExists = gpsExists,
            MaxSatCount = maxSatCount,
            Hdop = hdopAvg,
            HdopMin = hdopMin,
            HdopMax = hdopMax,
            HdopSampleCount = hdopCount,
            HdopHealth = health,
            HdopHealthLabel = healthLabel,
            SpoofDetected = spoofEvents.Count > 0,
            SpoofEvents = spoofEvents,
            StrongMagneticRadiationDetected = magRadiationEvents.Count > 0,
            MagRadiationEvents = magRadiationEvents,
            MoveMagnetometerAwayFromMotor = moveMagAway,
            MagThrottleCorrelation = magThrottleCorr,
            YawErrorGrowing = yawErrorGrowing,
            YawErrorAverageDeg = yawErrorAvg,
            YawCogHealth = yawCogHealth,
            YawCogHealthLabel = yawCogLabel,
            YawCogDiffAverageDeg = yawCogDiffAvg,
            YawCogSampleCount = yawCogDiffSamples.Count,
        };
    }

    /// <summary>
    /// Attitude yaw vs GPS COG difference:
    /// &lt;10° good; 10–30° ok; &gt;30° bad.
    /// </summary>
    public static (string Health, string Label) ClassifyYawCog(double? averageDiffDeg, int sampleCount)
    {
        if (sampleCount < YawCogMinSamples || averageDiffDeg is not { } value || !double.IsFinite(value))
        {
            return ("Unknown", "Attitude yaw / GPS course comparison unavailable");
        }

        if (value < 10)
        {
            return ("Good", "Attitude yaw and GPS course agree well");
        }

        if (value <= 30)
        {
            return ("Ok", "Attitude yaw and GPS course moderately disagree");
        }

        return ("Bad", "Attitude yaw and GPS course disagree significantly");
    }

    /// <summary>
    /// HDOP bands:
    /// 0–0.1 red unhealthy; 0.1–0.35 orange possibly unhealthy; 0.35–0.75 green healthy;
    /// 0.75–1.5 orange possibly unhealthy; 1.5+ red unhealthy.
    /// </summary>
    public static (string Health, string Label) ClassifyHdop(double? hdop)
    {
        if (hdop is not { } value || !double.IsFinite(value))
        {
            return ("Unknown", "Unknown");
        }

        if (value < 0.1)
        {
            return ("Unhealthy", "Unhealthy GPS");
        }

        if (value < 0.35)
        {
            return ("PossiblyUnhealthy", "Possibly unhealthy GPS");
        }

        if (value < 0.75)
        {
            return ("Healthy", "Healthy GPS");
        }

        if (value < 1.5)
        {
            return ("PossiblyUnhealthy", "Possibly unhealthy GPS");
        }

        return ("Unhealthy", "Unhealthy GPS");
    }

    private static bool IsGrowingThroughFlight(
        IReadOnlyList<double> samples,
        int minSamples,
        double growthDelta)
    {
        if (samples.Count < minSamples)
        {
            return false;
        }

        var quarter = Math.Max(1, samples.Count / 4);
        var firstAvg = samples.Take(quarter).Average();
        var lastAvg = samples.Skip(samples.Count - quarter).Average();
        return lastAvg - firstAvg >= growthDelta;
    }

    private static double AbsoluteHeadingErrorDeg(double headingDeg)
    {
        var normalized = NormalizeHeading(headingDeg);
        return Math.Min(normalized, 360.0 - normalized);
    }

    private static double AbsoluteHeadingDeltaDeg(double aDeg, double bDeg) =>
        AbsoluteHeadingErrorDeg(aDeg - bDeg);

    private static bool IsPlausibleHeading(double degrees) =>
        double.IsFinite(degrees) && degrees > -720 && degrees < 720;

    private static double NormalizeHeading(double degrees)
    {
        var normalized = degrees % 360.0;
        if (normalized < 0)
        {
            normalized += 360.0;
        }

        return normalized;
    }

    private static bool TryReadThrottle(IReadOnlyDictionary<string, object> atMs, out double throttle)
    {
        if (TryAsDouble(atMs, ThrottleCh3Key, out throttle))
        {
            return true;
        }

        return TryAsDouble(atMs, ThrottlePercentKey, out throttle);
    }

    private static double? TryPearsonCorrelation(IReadOnlyList<double> xs, IReadOnlyList<double> ys)
    {
        var n = Math.Min(xs.Count, ys.Count);
        if (n < MagThrottleMinSamples)
        {
            return null;
        }

        double sumX = 0, sumY = 0, sumXX = 0, sumYY = 0, sumXY = 0;
        for (var i = 0; i < n; i++)
        {
            var x = xs[i];
            var y = ys[i];
            sumX += x;
            sumY += y;
            sumXX += x * x;
            sumYY += y * y;
            sumXY += x * y;
        }

        var cov = n * sumXY - sumX * sumY;
        var varX = n * sumXX - sumX * sumX;
        var varY = n * sumYY - sumY * sumY;
        if (varX <= 1e-9 || varY <= 1e-9)
        {
            return null;
        }

        var r = cov / Math.Sqrt(varX * varY);
        return double.IsFinite(r) ? r : null;
    }

    private static int ReadSatCount(IReadOnlyDictionary<string, object> atMs)
    {
        var max = 0;
        if (TryAsDouble(atMs, SatCountKey, out var s1) && s1 > max)
        {
            max = (int)Math.Round(s1);
        }

        if (TryAsDouble(atMs, SatCount2Key, out var s2) && s2 > max)
        {
            max = (int)Math.Round(s2);
        }

        if (TryAsDouble(atMs, GpsInputSatCountKey, out var s3) && s3 > max)
        {
            max = (int)Math.Round(s3);
        }

        return max;
    }

    private static bool TryReadPlaneCoordinate(
        IReadOnlyDictionary<string, object> atMs,
        out double lat,
        out double lon)
    {
        if (TryReadCoordinate(atMs, AliasLatKey, AliasLonKey, out lat, out lon))
        {
            return true;
        }

        if (TryReadCoordinate(atMs, GlobalPosLatKey, GlobalPosLonKey, out lat, out lon))
        {
            return true;
        }

        return TryReadCoordinate(atMs, GpsRawLatKey, GpsRawLonKey, out lat, out lon);
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
        if (!TryAsDouble(fields, latKey, out latitudeDeg)
            || !TryAsDouble(fields, lonKey, out longitudeDeg))
        {
            return false;
        }

        return !(Math.Abs(latitudeDeg) < 1e-9 && Math.Abs(longitudeDeg) < 1e-9);
    }

    private static bool TryAsDouble(
        IReadOnlyDictionary<string, object> fields,
        string key,
        out double result)
    {
        result = 0;
        return fields.TryGetValue(key, out var value) && TryAsDouble(value, out result);
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

    private static string FormatUtc(long ms) =>
        DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

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
}
