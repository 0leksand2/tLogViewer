namespace tLogViewer.DTO.Messages;

public sealed class VfrHudData
{
    public float AirspeedMS { get; init; }
    public float GroundSpeedMS { get; init; }
    public short HeadingDeg { get; init; }
    public ushort ThrottlePct { get; init; }
    public float AltitudeM { get; init; }
    public float ClimbMS { get; init; }
}
