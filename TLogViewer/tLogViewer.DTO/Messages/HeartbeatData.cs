namespace tLogViewer.DTO.Messages;

public sealed class HeartbeatData
{
    public uint CustomMode { get; init; }
    public required string AircraftType { get; init; }
    public required string Autopilot { get; init; }
    public required string SystemStatus { get; init; }
    public bool Armed { get; init; }
    public int BaseMode { get; init; }
    public byte MavlinkVersion { get; init; }
}
