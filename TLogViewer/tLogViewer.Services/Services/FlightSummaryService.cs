using tLogViewer.Core.Enums.Heartbeat;
using tLogViewer.Core.Models;

namespace tLogViewer.Services.Services;

/// <summary>
/// Analyzes flight timeline fields for GPS presence (by sat count), HDOP health,
/// magnetometer anomalies, spoof jumps (&gt; 5 km), and RC stick channel usage
/// during manual / stick-driven flight modes.
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
    private const double StickPwmCenter = 1500.0;
    private const double StickPwmHalfRange = 500.0;
    private const double StickPwmMinValid = 800.0;
    private const double StickPwmMaxValid = 2200.0;
    private const double StickUsageGoodMaxPct = 30.0;
    private const double StickUsageImproveMaxPct = 60.0;
    /// <summary>ArduPilot VIBE: below 30 m/s/s normally acceptable.</summary>
    private const double VibeHealthyMaxMs2 = 30.0;
    /// <summary>ArduPilot VIBE: above 60 m/s/s nearly always causes problems.</summary>
    private const double VibeBadMinMs2 = 60.0;
    /// <summary>Clip counters: 0 ideal; &lt;100 often OK (e.g. hard landing).</summary>
    private const long ClipWarnMaxDelta = 100;
    /// <summary>Peak |a| in g before warning (16 g is typical IMU clip limit).</summary>
    private const double AccelWarnPeakG = 4.0;
    private const double AccelBadPeakG = 8.0;
    /// <summary>Peak gyro magnitude in rad/s.</summary>
    private const double GyroWarnPeakRadS = 5.0;
    private const double GyroBadPeakRadS = 15.0;

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
    private static readonly string CustomModeKey = FlightFieldIds.CustomMode;
    private static readonly string AccelXKey = FlightFieldIds.Ax;
    private static readonly string AccelYKey = FlightFieldIds.Ay;
    private static readonly string AccelZKey = FlightFieldIds.Az;
    private static readonly string GyroXKey = FlightFieldIds.Gx;
    private static readonly string GyroYKey = FlightFieldIds.Gy;
    private static readonly string GyroZKey = FlightFieldIds.Gz;
    private static readonly string VibeXKey = FlightFieldIds.VibeX;
    private static readonly string VibeYKey = FlightFieldIds.VibeY;
    private static readonly string VibeZKey = FlightFieldIds.VibeZ;
    private static readonly string Clip0Key = FlightFieldIds.VibeClip0;
    private static readonly string Clip1Key = FlightFieldIds.VibeClip1;
    private static readonly string Clip2Key = FlightFieldIds.VibeClip2;

    /// <summary>
    /// Modes where the pilot flies with sticks (not mission / guided autopilot).
    /// </summary>
    private static readonly HashSet<uint> ManualStickModes =
    [
        (uint)FlightMode.MANUAL,
        (uint)FlightMode.STABILIZE,
        (uint)FlightMode.TRAINING,
        (uint)FlightMode.ACRO,
        (uint)FlightMode.FBWA,
        (uint)FlightMode.FBWB,
        (uint)FlightMode.CRUISE,
        (uint)FlightMode.AUTOTUNE,
        (uint)FlightMode.QSTABILIZE,
        (uint)FlightMode.QHOVER,
    ];

    private static readonly (string Name, string Key)[] MagFields =
    [
        ("MagX", MagXKey),
        ("MagY", MagYKey),
        ("MagZ", MagZKey),
        ("MagField", MagFieldKey),
    ];

    private static readonly (int Channel, string Name, string Key)[] StickChannels =
    [
        (1, "Roll", FlightFieldIds.Ch1in),
        (2, "Pitch", FlightFieldIds.Ch2in),
        (3, "Throttle", FlightFieldIds.Ch3in),
        (4, "Yaw", FlightFieldIds.Ch4in),
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
        var stickAccumulators = StickChannels
            .Select(static c => new StickUsageAccumulator(c.Channel, c.Name, c.Key))
            .ToArray();
        uint? currentFlightMode = null;

        var accelMagSum = 0.0;
        var accelCount = 0;
        double accelPeakMag = 0;
        double accelPeakAbsX = 0;
        double accelPeakAbsY = 0;
        double accelPeakAbsZ = 0;

        var gyroMagSum = 0.0;
        var gyroCount = 0;
        double gyroPeakMag = 0;
        double gyroPeakAbsX = 0;
        double gyroPeakAbsY = 0;
        double gyroPeakAbsZ = 0;

        var vibeMaxSum = 0.0;
        var vibeCount = 0;
        double vibePeak = 0;
        double vibePeakX = 0;
        double vibePeakY = 0;
        double vibePeakZ = 0;

        double? clip0First = null;
        double? clip0Last = null;
        double? clip1First = null;
        double? clip1Last = null;
        double? clip2First = null;
        double? clip2Last = null;
        var clipCount = 0;

        foreach (var ms in byMillisecond.Keys.OrderBy(static key => key))
        {
            var atMs = byMillisecond[ms];

            if (TryReadCustomMode(atMs, out var mode))
            {
                currentFlightMode = mode;
            }

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

            if (currentFlightMode is { } activeMode && ManualStickModes.Contains(activeMode))
            {
                foreach (var stick in stickAccumulators)
                {
                    if (TryAsDouble(atMs, stick.FieldKey, out var pwm) && IsValidStickPwm(pwm))
                    {
                        stick.Add(pwm);
                    }
                }
            }

            if (TryReadAccelG(atMs, out var ax, out var ay, out var az))
            {
                var mag = Math.Sqrt(ax * ax + ay * ay + az * az);
                accelCount++;
                accelMagSum += mag;
                accelPeakMag = Math.Max(accelPeakMag, mag);
                accelPeakAbsX = Math.Max(accelPeakAbsX, Math.Abs(ax));
                accelPeakAbsY = Math.Max(accelPeakAbsY, Math.Abs(ay));
                accelPeakAbsZ = Math.Max(accelPeakAbsZ, Math.Abs(az));
            }

            if (TryReadGyroRadS(atMs, out var gx, out var gy, out var gz))
            {
                var mag = Math.Sqrt(gx * gx + gy * gy + gz * gz);
                gyroCount++;
                gyroMagSum += mag;
                gyroPeakMag = Math.Max(gyroPeakMag, mag);
                gyroPeakAbsX = Math.Max(gyroPeakAbsX, Math.Abs(gx));
                gyroPeakAbsY = Math.Max(gyroPeakAbsY, Math.Abs(gy));
                gyroPeakAbsZ = Math.Max(gyroPeakAbsZ, Math.Abs(gz));
            }

            if (TryAsDouble(atMs, VibeXKey, out var vx)
                && TryAsDouble(atMs, VibeYKey, out var vy)
                && TryAsDouble(atMs, VibeZKey, out var vz)
                && IsFiniteNonNegative(vx)
                && IsFiniteNonNegative(vy)
                && IsFiniteNonNegative(vz))
            {
                var sampleMax = Math.Max(vx, Math.Max(vy, vz));
                vibeCount++;
                vibeMaxSum += sampleMax;
                vibePeak = Math.Max(vibePeak, sampleMax);
                vibePeakX = Math.Max(vibePeakX, vx);
                vibePeakY = Math.Max(vibePeakY, vy);
                vibePeakZ = Math.Max(vibePeakZ, vz);
            }

            if (TryAsDouble(atMs, Clip0Key, out var c0)
                && TryAsDouble(atMs, Clip1Key, out var c1)
                && TryAsDouble(atMs, Clip2Key, out var c2)
                && c0 >= 0
                && c1 >= 0
                && c2 >= 0)
            {
                clipCount++;
                clip0First ??= c0;
                clip1First ??= c1;
                clip2First ??= c2;
                clip0Last = c0;
                clip1Last = c1;
                clip2Last = c2;
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
        var imu = BuildImuSummary(
            accelCount,
            accelMagSum,
            accelPeakMag,
            accelPeakAbsX,
            accelPeakAbsY,
            accelPeakAbsZ,
            gyroCount,
            gyroMagSum,
            gyroPeakMag,
            gyroPeakAbsX,
            gyroPeakAbsY,
            gyroPeakAbsZ,
            vibeCount,
            vibeMaxSum,
            vibePeak,
            vibePeakX,
            vibePeakY,
            vibePeakZ,
            clipCount,
            clip0First,
            clip0Last,
            clip1First,
            clip1Last,
            clip2First,
            clip2Last);

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
            StickChannels = stickAccumulators.Select(static s => s.ToReport()).ToArray(),
            Imu = imu,
        };
    }

    private static FlightImuSummary BuildImuSummary(
        int accelCount,
        double accelMagSum,
        double accelPeakMag,
        double accelPeakAbsX,
        double accelPeakAbsY,
        double accelPeakAbsZ,
        int gyroCount,
        double gyroMagSum,
        double gyroPeakMag,
        double gyroPeakAbsX,
        double gyroPeakAbsY,
        double gyroPeakAbsZ,
        int vibeCount,
        double vibeMaxSum,
        double vibePeak,
        double vibePeakX,
        double vibePeakY,
        double vibePeakZ,
        int clipCount,
        double? clip0First,
        double? clip0Last,
        double? clip1First,
        double? clip1Last,
        double? clip2First,
        double? clip2Last)
    {
        double? accelAvg = accelCount > 0 ? Round1(accelMagSum / accelCount) : null;
        double? accelPeak = accelCount > 0 ? Round1(accelPeakMag) : null;
        var (accelHealth, accelLabel) = ClassifyAccel(accelPeak, accelCount);

        double? gyroAvg = gyroCount > 0 ? Round2(gyroMagSum / gyroCount) : null;
        double? gyroPeak = gyroCount > 0 ? Round2(gyroPeakMag) : null;
        var (gyroHealth, gyroLabel) = ClassifyGyro(gyroPeak, gyroCount);

        double? vibeAvg = vibeCount > 0 ? Round1(vibeMaxSum / vibeCount) : null;
        double? vibePeakR = vibeCount > 0 ? Round1(vibePeak) : null;
        var (vibeHealth, vibeLabel) = ClassifyVibration(vibePeakR, vibeAvg, vibeCount);

        var clip0 = ClipDelta(clip0First, clip0Last);
        var clip1 = ClipDelta(clip1First, clip1Last);
        var clip2 = ClipDelta(clip2First, clip2Last);
        var clipTotal = clip0 + clip1 + clip2;
        var (clipHealth, clipLabel) = ClassifyClipping(clipTotal, clipCount);

        var (overall, overallLabel) = WorstImuHealth(
            (accelHealth, accelLabel),
            (gyroHealth, gyroLabel),
            (vibeHealth, vibeLabel),
            (clipHealth, clipLabel));

        return new FlightImuSummary
        {
            OverallHealth = overall,
            OverallHealthLabel = overallLabel,
            AccelAvgMagnitudeG = accelAvg,
            AccelPeakMagnitudeG = accelPeak,
            AccelPeakAbsXG = accelCount > 0 ? Round1(accelPeakAbsX) : null,
            AccelPeakAbsYG = accelCount > 0 ? Round1(accelPeakAbsY) : null,
            AccelPeakAbsZG = accelCount > 0 ? Round1(accelPeakAbsZ) : null,
            AccelSampleCount = accelCount,
            AccelHealth = accelHealth,
            AccelHealthLabel = accelLabel,
            GyroAvgMagnitudeRadS = gyroAvg,
            GyroPeakMagnitudeRadS = gyroPeak,
            GyroPeakAbsXRadS = gyroCount > 0 ? Round2(gyroPeakAbsX) : null,
            GyroPeakAbsYRadS = gyroCount > 0 ? Round2(gyroPeakAbsY) : null,
            GyroPeakAbsZRadS = gyroCount > 0 ? Round2(gyroPeakAbsZ) : null,
            GyroSampleCount = gyroCount,
            GyroHealth = gyroHealth,
            GyroHealthLabel = gyroLabel,
            VibeAvgMaxMs2 = vibeAvg,
            VibePeakMs2 = vibePeakR,
            VibePeakXMs2 = vibeCount > 0 ? Round1(vibePeakX) : null,
            VibePeakYMs2 = vibeCount > 0 ? Round1(vibePeakY) : null,
            VibePeakZMs2 = vibeCount > 0 ? Round1(vibePeakZ) : null,
            VibeSampleCount = vibeCount,
            VibeHealth = vibeHealth,
            VibeHealthLabel = vibeLabel,
            Clip0Delta = clip0,
            Clip1Delta = clip1,
            Clip2Delta = clip2,
            ClipTotalDelta = clipTotal,
            ClipSampleCount = clipCount,
            ClipHealth = clipHealth,
            ClipHealthLabel = clipLabel,
        };
    }

    private static long ClipDelta(double? first, double? last)
    {
        if (first is not { } f || last is not { } l)
        {
            return 0;
        }

        var delta = l - f;
        return delta > 0 ? (long)Math.Round(delta) : 0;
    }

    /// <summary>
    /// ArduPilot VIBE guidance: &lt;30 m/s/s OK; 30–60 may have problems; &gt;60 nearly always bad.
    /// </summary>
    public static (string Health, string Label) ClassifyVibration(
        double? peakMs2,
        double? avgMaxMs2,
        int sampleCount)
    {
        if (sampleCount <= 0 || peakMs2 is not { } peak || !double.IsFinite(peak))
        {
            return ("Unknown", "No vibration samples");
        }

        var score = Math.Max(peak, avgMaxMs2 ?? 0);
        if (score < VibeHealthyMaxMs2)
        {
            return ("Healthy", "Vibration within ArduPilot acceptable range");
        }

        if (score < VibeBadMinMs2)
        {
            return ("Warn", "Elevated vibration — may affect position / altitude hold");
        }

        return ("Bad", "Severe vibration — position / altitude hold problems likely");
    }

    /// <summary>
    /// Clip counters should stay near 0; &lt;100 often OK; steadily rising / large delta is serious.
    /// </summary>
    public static (string Health, string Label) ClassifyClipping(long totalDelta, int sampleCount)
    {
        if (sampleCount <= 0)
        {
            return ("Unknown", "No clipping samples");
        }

        if (totalDelta <= 0)
        {
            return ("Healthy", "No accelerometer clipping during flight");
        }

        if (totalDelta < ClipWarnMaxDelta)
        {
            return ("Warn", "Some accelerometer clipping (often hard landings)");
        }

        return ("Bad", "Significant accelerometer clipping — fix mechanical vibration");
    }

    /// <summary>Peak accel magnitude vs typical 16 g IMU limit.</summary>
    public static (string Health, string Label) ClassifyAccel(double? peakMagnitudeG, int sampleCount)
    {
        if (sampleCount <= 0 || peakMagnitudeG is not { } peak || !double.IsFinite(peak))
        {
            return ("Unknown", "No accelerometer samples");
        }

        if (peak < AccelWarnPeakG)
        {
            return ("Healthy", "Acceleration peaks within normal flight range");
        }

        if (peak < AccelBadPeakG)
        {
            return ("Warn", "High acceleration peaks — check maneuvers / mounting");
        }

        return ("Bad", "Very high acceleration — near IMU saturation risk");
    }

    /// <summary>Peak gyro magnitude bands for unusual rates / noise.</summary>
    public static (string Health, string Label) ClassifyGyro(double? peakMagnitudeRadS, int sampleCount)
    {
        if (sampleCount <= 0 || peakMagnitudeRadS is not { } peak || !double.IsFinite(peak))
        {
            return ("Unknown", "No gyroscope samples");
        }

        if (peak < GyroWarnPeakRadS)
        {
            return ("Healthy", "Gyro rates within normal flight range");
        }

        if (peak < GyroBadPeakRadS)
        {
            return ("Warn", "Elevated gyro rates — check aggressive flight / vibration");
        }

        return ("Bad", "Extreme gyro rates — verify IMU / airframe integrity");
    }

    private static (string Health, string Label) WorstImuHealth(
        params (string Health, string Label)[] parts)
    {
        static int Rank(string health) => health switch
        {
            "Bad" => 3,
            "Warn" => 2,
            "Healthy" => 1,
            _ => 0,
        };

        var worst = parts.OrderByDescending(static p => Rank(p.Health)).First();
        if (worst.Health is "Unknown" && parts.Any(static p => p.Health is not "Unknown"))
        {
            worst = parts.Where(static p => p.Health is not "Unknown")
                .OrderByDescending(static p => Rank(p.Health))
                .First();
        }

        return worst.Health switch
        {
            "Healthy" => ("Healthy", "IMU looks healthy"),
            "Warn" => ("Warn", "IMU has warnings — review vibration / clipping / rates"),
            "Bad" => ("Bad", "IMU issues detected — vibration isolation or hardware check needed"),
            _ => ("Unknown", "IMU data unavailable"),
        };
    }

    private static bool TryReadAccelG(
        IReadOnlyDictionary<string, object> atMs,
        out double ax,
        out double ay,
        out double az)
    {
        ax = ay = az = 0;
        if (!TryAsDouble(atMs, AccelXKey, out ax)
            || !TryAsDouble(atMs, AccelYKey, out ay)
            || !TryAsDouble(atMs, AccelZKey, out az))
        {
            return false;
        }

        // SCALED_IMU stores g; RAW_IMU often stores mG (~1000 at 1 g).
        if (Math.Abs(ax) > 50 || Math.Abs(ay) > 50 || Math.Abs(az) > 50)
        {
            ax /= 1000.0;
            ay /= 1000.0;
            az /= 1000.0;
        }

        return double.IsFinite(ax) && double.IsFinite(ay) && double.IsFinite(az);
    }

    private static bool TryReadGyroRadS(
        IReadOnlyDictionary<string, object> atMs,
        out double gx,
        out double gy,
        out double gz)
    {
        gx = gy = gz = 0;
        if (!TryAsDouble(atMs, GyroXKey, out gx)
            || !TryAsDouble(atMs, GyroYKey, out gy)
            || !TryAsDouble(atMs, GyroZKey, out gz))
        {
            return false;
        }

        // SCALED_IMU stores rad/s; RAW_IMU often stores mrad/s.
        if (Math.Abs(gx) > 50 || Math.Abs(gy) > 50 || Math.Abs(gz) > 50)
        {
            gx /= 1000.0;
            gy /= 1000.0;
            gz /= 1000.0;
        }

        return double.IsFinite(gx) && double.IsFinite(gy) && double.IsFinite(gz);
    }

    private static bool IsFiniteNonNegative(double value) =>
        double.IsFinite(value) && value >= 0;

    private static double Round1(double value) => Math.Round(value, 1);

    private static double Round2(double value) => Math.Round(value, 2);

    private static bool IsValidStickPwm(double pwm) =>
        double.IsFinite(pwm) && pwm >= StickPwmMinValid && pwm <= StickPwmMaxValid;

    /// <summary>
    /// Average stick deflection bands:
    /// &lt;30% good planned flight; 30–60% room to improve planning; &gt;60% uncontrolled.
    /// </summary>
    public static (string Health, string Label) ClassifyStickUsage(double? averageUsagePercent, int sampleCount)
    {
        if (sampleCount <= 0 || averageUsagePercent is not { } value || !double.IsFinite(value))
        {
            return ("Unknown", "No manual-mode stick samples");
        }

        if (value < StickUsageGoodMaxPct)
        {
            return ("Good", "Good planned flight");
        }

        if (value < StickUsageImproveMaxPct)
        {
            return ("Improve", "Room to improve route planning");
        }

        return ("Uncontrolled", "Uncontrolled flight — pilot needs more training");
    }

    private static bool TryReadCustomMode(IReadOnlyDictionary<string, object> fields, out uint mode)
    {
        mode = 0;
        if (!fields.TryGetValue(CustomModeKey, out var value))
        {
            return false;
        }

        switch (value)
        {
            case uint u:
                mode = u;
                return true;
            case int i when i >= 0:
                mode = (uint)i;
                return true;
            case long l when l >= 0 && l <= uint.MaxValue:
                mode = (uint)l;
                return true;
            case ulong ul when ul <= uint.MaxValue:
                mode = (uint)ul;
                return true;
            case double d when double.IsFinite(d) && d >= 0 && d <= uint.MaxValue:
                mode = (uint)d;
                return true;
            case float f when float.IsFinite(f) && f >= 0 && f <= uint.MaxValue:
                mode = (uint)f;
                return true;
            default:
                return false;
        }
    }

    private sealed class StickUsageAccumulator
    {
        private readonly int _channel;
        private readonly string _name;
        private int _count;
        private double _deflectionSum;
        private double _maxDeflection;
        private double? _pwmMin;
        private double? _pwmMax;

        public StickUsageAccumulator(int channel, string name, string fieldKey)
        {
            _channel = channel;
            _name = name;
            FieldKey = fieldKey;
        }

        public string FieldKey { get; }

        public void Add(double pwm)
        {
            var deflection = Math.Min(1.0, Math.Abs(pwm - StickPwmCenter) / StickPwmHalfRange);
            _count++;
            _deflectionSum += deflection;
            if (deflection > _maxDeflection)
            {
                _maxDeflection = deflection;
            }

            _pwmMin = _pwmMin is null ? pwm : Math.Min(_pwmMin.Value, pwm);
            _pwmMax = _pwmMax is null ? pwm : Math.Max(_pwmMax.Value, pwm);
        }

        public FlightStickChannelUsage ToReport()
        {
            var averagePct = _count > 0
                ? Math.Round((_deflectionSum / _count) * 100.0, 1)
                : 0;
            var (health, label) = ClassifyStickUsage(averagePct, _count);

            return new FlightStickChannelUsage
            {
                Channel = _channel,
                Name = _name,
                FieldKey = FieldKey,
                UsagePercent = _count > 0 ? Math.Round(_maxDeflection * 100.0, 1) : 0,
                AverageUsagePercent = averagePct,
                UsageHealth = health,
                UsageHealthLabel = label,
                SampleCount = _count,
                PwmMin = _pwmMin,
                PwmMax = _pwmMax,
            };
        }
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
