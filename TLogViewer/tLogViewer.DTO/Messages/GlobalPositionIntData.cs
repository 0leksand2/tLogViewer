namespace tLogViewer.DTO.Messages;

public sealed class GlobalPositionIntData
{
    public uint TimeBootMs { get; init; }
    public double LatitudeDeg { get; init; }
    public double LongitudeDeg { get; init; }
    public double AslAltitudeM { get; init; }
    public double RelativeAltitudeM { get; init; }
    public double HorizontalVelocityMS { get; init; }
    public double VerticalVelocityMS { get; init; }
    public double HeadingDeg { get; init; }
}
