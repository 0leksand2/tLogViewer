namespace tLogViewer.Core.Models;

/// <summary>Timestamp when HEARTBEAT armed state changed during a flight.</summary>
public sealed class FlightArmChangePoint
{
    public long ChangedAtMs { get; init; }
    /// <summary><c>true</c> = armed, <c>false</c> = disarmed.</summary>
    public bool Armed { get; init; }
}
