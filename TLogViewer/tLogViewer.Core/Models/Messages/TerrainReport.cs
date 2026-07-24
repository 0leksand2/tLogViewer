namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink TERRAIN_REPORT (136).</summary>
public sealed class TerrainReport : MavLinkMessage
{
    public int Latitude;
    public int Longitude;
    public float TerrainHeightM;
    public float CurrentHeightM;
    public ushort SpacingM;
    public ushort Pending;
    public ushort Loaded;

    public double LatitudeDeg;
    public double LongitudeDeg;

    public override int ExpectedLength => 22;

    public TerrainReport(MavPacket packet) : base(packet)
    {
        Latitude = BitConverter.ToInt32(FullPacket, 0);
        Longitude = BitConverter.ToInt32(FullPacket, 4);
        TerrainHeightM = BitConverter.ToSingle(FullPacket, 8);
        CurrentHeightM = BitConverter.ToSingle(FullPacket, 12);
        SpacingM = BitConverter.ToUInt16(FullPacket, 16);
        Pending = BitConverter.ToUInt16(FullPacket, 18);
        Loaded = BitConverter.ToUInt16(FullPacket, 20);

        LatitudeDeg = Latitude / 1e7;
        LongitudeDeg = Longitude / 1e7;
    }

    public override void Print() =>
        Console.WriteLine($"TERRAIN_REPORT: Lat={LatitudeDeg:F7}, Lon={LongitudeDeg:F7}, TerrainHeight={TerrainHeightM:F1}m, CurrentHeight={CurrentHeightM:F1}m");
}
