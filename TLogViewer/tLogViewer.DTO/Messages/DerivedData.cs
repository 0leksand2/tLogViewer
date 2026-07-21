namespace tLogViewer.DTO.Messages;

/// <summary>Synthetic DERIVED (998) payload — values computed from the log, not wire MAVLink.</summary>
public sealed class DerivedData
{
    /// <summary>GCS link quality 0–100, Mission Planner style (3 s rx/(rx+lost) window).</summary>
    public double LinkQualityGcs { get; init; }

    /// <summary>Seconds since the vehicle last armed; 0 when disarmed.</summary>
    public double TimeSinceArmSec { get; init; }
}
