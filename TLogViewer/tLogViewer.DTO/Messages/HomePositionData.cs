namespace tLogViewer.DTO.Messages;

public sealed class HomePositionData
{
    public double LatitudeDeg { get; init; }
    public double LongitudeDeg { get; init; }
    public double AltitudeM { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
}
