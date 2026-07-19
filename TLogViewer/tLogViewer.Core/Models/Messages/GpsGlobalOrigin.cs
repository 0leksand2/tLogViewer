namespace tLogViewer.Core.Models.Messages
{
    public class GpsGlobalOrigin : MavLinkMessage
    {
        public int Latitude;
        public int Longitude;
        public int Altitude;
        public ulong TimeUsec;

        public double LatitudeDeg;
        public double LongitudeDeg;
        public double AltitudeM;

        public override int ExpectedLength => 20;

        public GpsGlobalOrigin(MavPacket packet) : base(packet)
        {
            Latitude = BitConverter.ToInt32(FullPacket, 0);
            Longitude = BitConverter.ToInt32(FullPacket, 4);
            Altitude = BitConverter.ToInt32(FullPacket, 8);
            TimeUsec = BitConverter.ToUInt64(FullPacket, 12);

            LatitudeDeg = Latitude / 1e7;
            LongitudeDeg = Longitude / 1e7;
            AltitudeM = Altitude / 1000.0;
        }

        public override void Print()
        {
            Console.WriteLine($"GpsGlobalOrigin: Lat={LatitudeDeg:F7}, Lon={LongitudeDeg:F7}, Alt={AltitudeM:F1}m");
        }
    }
}
