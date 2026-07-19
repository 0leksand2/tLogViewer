using tLogViewer.Core.Enums.Gps;

namespace tLogViewer.Core.Models.Messages
{
    public class GpsRawInt : MavLinkMessage
    {
        public ulong TimeUsec;
        public int Latitude;
        public int Longitude;
        public int Altitude;
        public ushort Eph;
        public ushort Epv;
        public ushort Vel;
        public ushort Cog;
        public GpsFixType FixType;
        public byte SatellitesVisible;
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

        public override int ExpectedLength => 52;

        public GpsRawInt(MavPacket packet) : base(packet)
        {
            TimeUsec = BitConverter.ToUInt64(FullPacket, 0);
            Latitude = BitConverter.ToInt32(FullPacket, 8);
            Longitude = BitConverter.ToInt32(FullPacket, 12);
            Altitude = BitConverter.ToInt32(FullPacket, 16);
            Eph = BitConverter.ToUInt16(FullPacket, 20);
            Epv = BitConverter.ToUInt16(FullPacket, 22);
            Vel = BitConverter.ToUInt16(FullPacket, 24);
            Cog = BitConverter.ToUInt16(FullPacket, 26);
            FixType = Enum.IsDefined(typeof(GpsFixType), (int)FullPacket[28])
                ? (GpsFixType)FullPacket[28]
                : GpsFixType.NoGps;
            SatellitesVisible = FullPacket[29];
            AltEllipsoid = BitConverter.ToInt32(FullPacket, 30);
            HAcc = BitConverter.ToUInt32(FullPacket, 34);
            VAcc = BitConverter.ToUInt32(FullPacket, 38);
            VelAcc = BitConverter.ToUInt32(FullPacket, 42);
            HdgAcc = BitConverter.ToUInt32(FullPacket, 46);
            Yaw = BitConverter.ToUInt16(FullPacket, 50);

            LatitudeDeg = Latitude / 1e7;
            LongitudeDeg = Longitude / 1e7;
            AltitudeM = Altitude / 1000.0;
            GroundSpeedMS = Vel / 100.0;
            CourseOverGroundDeg = Cog / 100.0;
        }

        public override void Print()
        {
            Console.WriteLine($"GpsRawInt: Fix={FixType}, Lat={LatitudeDeg:F7}, Lon={LongitudeDeg:F7}, Alt={AltitudeM:F1}m, Sats={SatellitesVisible}, Spd={GroundSpeedMS:F1}m/s");
        }
    }
}
