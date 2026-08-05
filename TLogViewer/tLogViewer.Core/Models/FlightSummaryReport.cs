namespace tLogViewer.Core.Models;

/// <summary>GPS / magnetometer / spoof analysis for one flight (produced by <c>FlightSummaryService</c>).</summary>
public sealed class FlightSummaryReport
{
    /// <summary>True when the log reports a non-zero satellite count at least once.</summary>
    public bool GpsExists { get; init; }

    /// <summary>Highest satellites-visible sample seen in the flight.</summary>
    public int MaxSatCount { get; init; }

    /// <summary>Average GPS HDOP when samples exist; otherwise null.</summary>
    public double? Hdop { get; init; }

    public double? HdopMin { get; init; }
    public double? HdopMax { get; init; }
    public int HdopSampleCount { get; init; }

    /// <summary>Health band for average HDOP: Unhealthy, PossiblyUnhealthy, Healthy, or Unknown.</summary>
    public required string HdopHealth { get; init; }

    /// <summary>Human-readable HDOP health label.</summary>
    public required string HdopHealthLabel { get; init; }

    public bool SpoofDetected { get; init; }

    public required IReadOnlyList<FlightSpoofEvent> SpoofEvents { get; init; }

    /// <summary>True when MagX/Y/Z/MagField jumped &gt; 150 points between 2 consecutive samples.</summary>
    public bool StrongMagneticRadiationDetected { get; init; }

    public required IReadOnlyList<FlightMagRadiationEvent> MagRadiationEvents { get; init; }

    /// <summary>True when MagField rises together with throttle (ch3).</summary>
    public bool MoveMagnetometerAwayFromMotor { get; init; }

    /// <summary>Pearson correlation of MagField vs throttle when enough paired samples exist.</summary>
    public double? MagThrottleCorrelation { get; init; }

    /// <summary>True when absolute yaw/bearing error (ber_error) trends upward over the flight.</summary>
    public bool YawErrorGrowing { get; init; }

    /// <summary>Average absolute yaw/bearing error in degrees when samples exist.</summary>
    public double? YawErrorAverageDeg { get; init; }

    /// <summary>
    /// Attitude yaw vs GPS_RAW_INT course-over-ground agreement:
    /// Good (&lt;10°), Ok (10–30°), Bad (&gt;30°), or Unknown.
    /// </summary>
    public required string YawCogHealth { get; init; }

    public required string YawCogHealthLabel { get; init; }

    /// <summary>Average absolute circular difference between attitude yaw and GPS COG (deg).</summary>
    public double? YawCogDiffAverageDeg { get; init; }

    public int YawCogSampleCount { get; init; }

    /// <summary>
    /// RC stick channels 1–4 (ch1in–ch4in) usage vs center PWM 1500.
    /// Always length 4; <see cref="FlightStickChannelUsage.SampleCount"/> is 0 when no data.
    /// </summary>
    public required IReadOnlyList<FlightStickChannelUsage> StickChannels { get; init; }

    /// <summary>IMU acceleration / gyro / vibration / clipping analysis (ArduPilot VIBE guidance).</summary>
    public required FlightImuSummary Imu { get; init; }
}

/// <summary>IMU health summary for one flight.</summary>
public sealed class FlightImuSummary
{
    /// <summary>Worst of accel / gyro / vibration / clipping: Healthy, Warn, Bad, or Unknown.</summary>
    public required string OverallHealth { get; init; }

    public required string OverallHealthLabel { get; init; }

    public double? AccelAvgMagnitudeG { get; init; }
    public double? AccelPeakMagnitudeG { get; init; }
    public double? AccelPeakAbsXG { get; init; }
    public double? AccelPeakAbsYG { get; init; }
    public double? AccelPeakAbsZG { get; init; }
    public int AccelSampleCount { get; init; }
    public required string AccelHealth { get; init; }
    public required string AccelHealthLabel { get; init; }

    public double? GyroAvgMagnitudeRadS { get; init; }
    public double? GyroPeakMagnitudeRadS { get; init; }
    public double? GyroPeakAbsXRadS { get; init; }
    public double? GyroPeakAbsYRadS { get; init; }
    public double? GyroPeakAbsZRadS { get; init; }
    public int GyroSampleCount { get; init; }
    public required string GyroHealth { get; init; }
    public required string GyroHealthLabel { get; init; }

    /// <summary>Average of per-sample max(VibeX, VibeY, VibeZ) in m/s/s.</summary>
    public double? VibeAvgMaxMs2 { get; init; }

    /// <summary>Peak of any vibe axis in m/s/s.</summary>
    public double? VibePeakMs2 { get; init; }

    public double? VibePeakXMs2 { get; init; }
    public double? VibePeakYMs2 { get; init; }
    public double? VibePeakZMs2 { get; init; }
    public int VibeSampleCount { get; init; }
    public required string VibeHealth { get; init; }
    public required string VibeHealthLabel { get; init; }

    public long Clip0Delta { get; init; }
    public long Clip1Delta { get; init; }
    public long Clip2Delta { get; init; }
    public long ClipTotalDelta { get; init; }
    public int ClipSampleCount { get; init; }
    public required string ClipHealth { get; init; }
    public required string ClipHealthLabel { get; init; }
}

/// <summary>Stick travel usage for one RC input channel relative to PWM center 1500.</summary>
public sealed class FlightStickChannelUsage
{
    /// <summary>Channel number 1–4 (Mission Planner chNin).</summary>
    public int Channel { get; init; }

    /// <summary>Roll, Pitch, Throttle, or Yaw.</summary>
    public required string Name { get; init; }

    /// <summary>Flattened field key (e.g. 65_005).</summary>
    public required string FieldKey { get; init; }

    /// <summary>Peak |PWM − 1500| / 500 as percent of full stick travel (0–100).</summary>
    public double UsagePercent { get; init; }

    /// <summary>Mean |PWM − 1500| / 500 as percent of full stick travel (0–100). Primary metric.</summary>
    public double AverageUsagePercent { get; init; }

    /// <summary>Good (&lt;30%), Improve (30–60%), Uncontrolled (&gt;60%), or Unknown.</summary>
    public required string UsageHealth { get; init; }

    /// <summary>Human-readable usage health label.</summary>
    public required string UsageHealthLabel { get; init; }

    public int SampleCount { get; init; }

    public double? PwmMin { get; init; }
    public double? PwmMax { get; init; }
}

/// <summary>A coordinate jump larger than the spoof threshold.</summary>
public sealed class FlightSpoofEvent
{
    /// <summary>Unix epoch millisecond when the jump was observed.</summary>
    public long TimestampMs { get; init; }

    /// <summary>ISO-8601 UTC timestamp for the jump.</summary>
    public required string TimestampUtc { get; init; }

    public double FromLatitudeDeg { get; init; }
    public double FromLongitudeDeg { get; init; }
    public double ToLatitudeDeg { get; init; }
    public double ToLongitudeDeg { get; init; }

    /// <summary>Haversine distance of the jump in meters.</summary>
    public double DistanceM { get; init; }
}

/// <summary>A magnetometer jump suggesting strong external magnetic radiation.</summary>
public sealed class FlightMagRadiationEvent
{
    public long TimestampMs { get; init; }
    public required string TimestampUtc { get; init; }

    /// <summary>MagX, MagY, MagZ, or MagField.</summary>
    public required string FieldName { get; init; }

    /// <summary>Absolute delta between the two consecutive samples.</summary>
    public double JumpPoints { get; init; }

    public double? LatitudeDeg { get; init; }
    public double? LongitudeDeg { get; init; }
}
