namespace tLogViewer.DTO.Messages;

/// <summary>Synthetic DERIVED (998) payload — values computed from the log, not wire MAVLink.</summary>
public sealed class DerivedData
{
    /// <summary>GCS link quality 0–100, Mission Planner style (3 s rx/(rx+lost) window).</summary>
    public double LinkQualityGcs { get; init; }

    /// <summary>Seconds since the vehicle last armed; 0 when disarmed.</summary>
    public double TimeSinceArmSec { get; init; }

    /// <summary>
    /// Bearing from home to the vehicle (degrees, 0–360). Populated per-ms by
    /// <c>DerivedFieldsEnricher</c>; optional on 1 Hz samples.
    /// </summary>
    public double? AzToMav { get; init; }

    /// <summary>
    /// Ground distance from home to the vehicle (meters). Populated per-ms by
    /// <c>DerivedFieldsEnricher</c>; optional on 1 Hz samples.
    /// </summary>
    public double? DistToHome { get; init; }
}
