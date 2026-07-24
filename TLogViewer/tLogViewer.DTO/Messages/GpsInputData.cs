namespace tLogViewer.DTO.Messages;

public sealed class GpsInputData
{
    public byte GpsId { get; init; }
    public byte FixType { get; init; }
    public double LatitudeDeg { get; init; }
    public double LongitudeDeg { get; init; }
    public float AltitudeM { get; init; }
    public byte SatellitesVisible { get; init; }
    public float Hdop { get; init; }
    public float Vdop { get; init; }
}
