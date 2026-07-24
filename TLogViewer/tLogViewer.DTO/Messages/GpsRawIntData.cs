namespace tLogViewer.DTO.Messages;

public sealed class GpsRawIntData
{
    public required string FixType { get; init; }
    public double LatitudeDeg { get; init; }
    public double LongitudeDeg { get; init; }
    public double AltitudeM { get; init; }
    public double GroundSpeedMS { get; init; }
    public double CourseOverGroundDeg { get; init; }
    public byte SatellitesVisible { get; init; }
    public ushort Eph { get; init; }
    public ushort Epv { get; init; }
    public uint? HAcc { get; init; }
    public uint? VAcc { get; init; }
    public uint? VelAcc { get; init; }
    public uint? HdgAcc { get; init; }
    public ushort? YawCdeg { get; init; }
}
