namespace tLogViewer.Core.Models.Messages
{
    public class GlobalPositionInt : MavLinkMessage
    {
        public uint TimeBootMs;
        public int Latitude;
        public int Longitude;
        public int Altitude;
        public int RelativeAltitude;
        public short VelocityX;
        public short VelocityY;
        public short VelocityZ;
        public ushort Heading;

        public double LattitudeDeg;
        public double LongitudeDeg;
        public double AslAltitudeM;
        public double RelativeAltitudeM;

        public double HorizontalVelocityMS;
        public double VerticalVelocityMS;
        public override int ExpectedLength => 28;

        public GlobalPositionInt(MavPacket packet) : base(packet)
        {
            TimeBootMs = BitConverter.ToUInt32(FullPacket, 0);
            Latitude = BitConverter.ToInt32(FullPacket, 4);
            Longitude = BitConverter.ToInt32(FullPacket, 8);
            Altitude = BitConverter.ToInt32(FullPacket, 12);
            RelativeAltitude = BitConverter.ToInt32(FullPacket, 16);
            VelocityX = BitConverter.ToInt16(FullPacket, 20);
            VelocityY = BitConverter.ToInt16(FullPacket, 22);
            VelocityZ = BitConverter.ToInt16(FullPacket, 24);
            Heading = BitConverter.ToUInt16(FullPacket, 26);

            LattitudeDeg = Latitude / 1e7;
            LongitudeDeg = Longitude / 1e7;
            AslAltitudeM = Altitude / 1000;
            RelativeAltitudeM = RelativeAltitude / 1000;
            HorizontalVelocityMS = VelocityX / 100;
            VerticalVelocityMS = VelocityZ / 100;
        }



        public override void Print()
        {
            Console.WriteLine($"GlobalPositionInt: Latitude={Latitude}, Longitude={Longitude}, Relative Altitude={RelativeAltitude}, Horizontal Velocity={HorizontalVelocityMS}, Vertical Velocity={VerticalVelocityMS}");
        }

    }
}
