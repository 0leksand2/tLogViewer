namespace tLogViewer.DTO.Messages;

public sealed class GpsStatusData
{
    public byte SatellitesVisible { get; init; }
    public required IReadOnlyList<byte> SatellitePrn { get; init; }
    public required IReadOnlyList<byte> SatelliteUsed { get; init; }
    public required IReadOnlyList<byte> SatelliteElevation { get; init; }
    public required IReadOnlyList<byte> SatelliteAzimuth { get; init; }
    public required IReadOnlyList<byte> SatelliteSnr { get; init; }
}
