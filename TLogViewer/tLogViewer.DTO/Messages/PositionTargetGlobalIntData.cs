namespace tLogViewer.DTO.Messages;

public sealed class PositionTargetGlobalIntData
{
    public uint TimeBootMs { get; init; }
    public double LatitudeDeg { get; init; }
    public double LongitudeDeg { get; init; }
    public float AltitudeM { get; init; }
    public float Vx { get; init; }
    public float Vy { get; init; }
    public float Vz { get; init; }
    public float Afx { get; init; }
    public float Afy { get; init; }
    public float Afz { get; init; }
    public float Yaw { get; init; }
    public double YawDeg { get; init; }
    public float YawRate { get; init; }
    public double YawRateDegS { get; init; }
    public ushort TypeMask { get; init; }
    public byte CoordinateFrame { get; init; }
}
