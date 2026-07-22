namespace tLogViewer.Core.Models;

/// <summary>Timestamp when HEARTBEAT customMode changed during a flight.</summary>
public sealed class FlightModeChangePoint
{
    public long ChangedAtMs { get; init; }
    public uint CustomMode { get; init; }
}
