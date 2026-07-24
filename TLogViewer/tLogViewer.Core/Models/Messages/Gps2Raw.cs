using tLogViewer.Core.Enums.Gps;

namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink GPS2_RAW (124).</summary>
public sealed class Gps2Raw : MavLinkMessage
{
    public ulong TimeUsec;
    public int Latitude;
    public int Longitude;
    public int Altitude;
    public uint DgpsAge;
    public ushort Eph;
    public ushort Epv;
    public ushort Vel;
    public ushort Cog;
    public GpsFixType FixType;
    public byte SatellitesVisible;
    public byte DgpsNumch;
    public int AltEllipsoid;
    public uint HAcc;
    public uint VAcc;
    public uint VelAcc;
    public uint HdgAcc;
    public ushort Yaw;

    public double LatitudeDeg;
    public double LongitudeDeg;
    public double AltitudeM;
    public double GroundSpeedMS;
    public double CourseOverGroundDeg;

    /// <summary>Base 35; extensions through yaw → 57.</summary>
    public override int ExpectedLength => 57;

    public Gps2Raw(MavPacket packet) : base(packet)
    {
        TimeUsec = BitConverter.ToUInt64(FullPacket, 0);
        Latitude = BitConverter.ToInt32(FullPacket, 8);
        Longitude = BitConverter.ToInt32(FullPacket, 12);
        Altitude = BitConverter.ToInt32(FullPacket, 16);
        DgpsAge = BitConverter.ToUInt32(FullPacket, 20);
        Eph = BitConverter.ToUInt16(FullPacket, 24);
        Epv = BitConverter.ToUInt16(FullPacket, 26);
        Vel = BitConverter.ToUInt16(FullPacket, 28);
        Cog = BitConverter.ToUInt16(FullPacket, 30);
        FixType = Enum.IsDefined(typeof(GpsFixType), (int)FullPacket[32])
            ? (GpsFixType)FullPacket[32]
            : GpsFixType.NoGps;
        SatellitesVisible = FullPacket[33];
        DgpsNumch = FullPacket[34];

        if (packet.Payload.Length >= 57)
        {
            AltEllipsoid = BitConverter.ToInt32(FullPacket, 35);
            HAcc = BitConverter.ToUInt32(FullPacket, 39);
            VAcc = BitConverter.ToUInt32(FullPacket, 43);
            VelAcc = BitConverter.ToUInt32(FullPacket, 47);
            HdgAcc = BitConverter.ToUInt32(FullPacket, 51);
            Yaw = BitConverter.ToUInt16(FullPacket, 55);
        }

        LatitudeDeg = Latitude / 1e7;
        LongitudeDeg = Longitude / 1e7;
        AltitudeM = Altitude / 1000.0;
        GroundSpeedMS = Vel / 100.0;
        CourseOverGroundDeg = Cog / 100.0;
    }

    public override void Print() =>
        Console.WriteLine($"GPS2_RAW: Fix={FixType}, Lat={LatitudeDeg:F7}, Sats={SatellitesVisible}");
}
