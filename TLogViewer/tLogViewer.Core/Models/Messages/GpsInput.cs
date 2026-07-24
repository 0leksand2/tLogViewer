namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink GPS_INPUT (232) — external GPS input to the autopilot.</summary>
public sealed class GpsInput : MavLinkMessage
{
    public ulong TimeUsec;
    public uint TimeWeekMs;
    public int Lat;
    public int Lon;
    public float AltM;
    public float Hdop;
    public float Vdop;
    public float Vn;
    public float Ve;
    public float Vd;
    public float SpeedAccuracy;
    public float HorizAccuracy;
    public float VertAccuracy;
    public ushort IgnoreFlags;
    public ushort TimeWeek;
    public byte GpsId;
    public byte FixType;
    public byte SatellitesVisible;
    public ushort YawCdeg;

    public double LatitudeDeg;
    public double LongitudeDeg;

    public override int ExpectedLength => 65;

    public GpsInput(MavPacket packet) : base(packet)
    {
        TimeUsec = BitConverter.ToUInt64(FullPacket, 0);
        TimeWeekMs = BitConverter.ToUInt32(FullPacket, 8);
        Lat = BitConverter.ToInt32(FullPacket, 12);
        Lon = BitConverter.ToInt32(FullPacket, 16);
        AltM = BitConverter.ToSingle(FullPacket, 20);
        Hdop = BitConverter.ToSingle(FullPacket, 24);
        Vdop = BitConverter.ToSingle(FullPacket, 28);
        Vn = BitConverter.ToSingle(FullPacket, 32);
        Ve = BitConverter.ToSingle(FullPacket, 36);
        Vd = BitConverter.ToSingle(FullPacket, 40);
        SpeedAccuracy = BitConverter.ToSingle(FullPacket, 44);
        HorizAccuracy = BitConverter.ToSingle(FullPacket, 48);
        VertAccuracy = BitConverter.ToSingle(FullPacket, 52);
        IgnoreFlags = BitConverter.ToUInt16(FullPacket, 56);
        TimeWeek = BitConverter.ToUInt16(FullPacket, 58);
        GpsId = FullPacket[60];
        FixType = FullPacket[61];
        SatellitesVisible = FullPacket[62];
        if (FullPacket.Length >= 65)
        {
            YawCdeg = BitConverter.ToUInt16(FullPacket, 63);
        }

        LatitudeDeg = Lat / 1e7;
        LongitudeDeg = Lon / 1e7;
    }

    public override void Print() =>
        Console.WriteLine(
            $"GPS_INPUT: Id={GpsId}, Fix={FixType}, Lat={LatitudeDeg:F7}, Lon={LongitudeDeg:F7}, Sats={SatellitesVisible}");
}
