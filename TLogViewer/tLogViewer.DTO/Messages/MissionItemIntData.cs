namespace tLogViewer.DTO.Messages;

public sealed class MissionItemIntData
{
    public ushort Seq { get; init; }
    public ushort Command { get; init; }
    public byte Frame { get; init; }
    public byte Current { get; init; }
    public byte Autocontinue { get; init; }
    public double LatitudeDeg { get; init; }
    public double LongitudeDeg { get; init; }
    public float AltitudeM { get; init; }
    public float Param1 { get; init; }
    public float Param2 { get; init; }
    public float Param3 { get; init; }
    public float Param4 { get; init; }
}
