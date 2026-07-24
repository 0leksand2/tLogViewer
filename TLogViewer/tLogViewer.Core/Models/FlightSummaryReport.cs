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
