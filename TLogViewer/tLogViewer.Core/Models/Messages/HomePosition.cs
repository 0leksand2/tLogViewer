namespace tLogViewer.Core.Models.Messages
{
    public class HomePosition : MavLinkMessage
    {
        public int Latitude;
        public int Longitude;
        public int Altitude;
        public float X;
        public float Y;
        public float Z;
        public float[] Q = new float[4];
        public float ApproachX;
        public float ApproachY;
        public float ApproachZ;
        public ulong TimeUsec;

        public double LatitudeDeg;
        public double LongitudeDeg;
        public double AltitudeM;

        public override int ExpectedLength => 60;

        public HomePosition(MavPacket packet) : base(packet)
        {
            Latitude = BitConverter.ToInt32(FullPacket, 0);
            Longitude = BitConverter.ToInt32(FullPacket, 4);
            Altitude = BitConverter.ToInt32(FullPacket, 8);
            X = BitConverter.ToSingle(FullPacket, 12);
            Y = BitConverter.ToSingle(FullPacket, 16);
            Z = BitConverter.ToSingle(FullPacket, 20);
            Q[0] = BitConverter.ToSingle(FullPacket, 24);
            Q[1] = BitConverter.ToSingle(FullPacket, 28);
            Q[2] = BitConverter.ToSingle(FullPacket, 32);
            Q[3] = BitConverter.ToSingle(FullPacket, 36);
            ApproachX = BitConverter.ToSingle(FullPacket, 40);
            ApproachY = BitConverter.ToSingle(FullPacket, 44);
            ApproachZ = BitConverter.ToSingle(FullPacket, 48);
            TimeUsec = BitConverter.ToUInt64(FullPacket, 52);

            LatitudeDeg = Latitude / 1e7;
            LongitudeDeg = Longitude / 1e7;
            AltitudeM = Altitude / 1000.0;
        }

        public override void Print()
        {
            Console.WriteLine($"HomePosition: Lat={LatitudeDeg:F7}, Lon={LongitudeDeg:F7}, Alt={AltitudeM:F1}m");
        }
    }
}
