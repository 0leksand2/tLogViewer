namespace tLogViewer.Core.Models.Messages
{
    public class VfrHud : MavLinkMessage
    {
        public float Airspeed;

        public float GroundSpeed;

        public short Heading;

        public ushort Throttle;

        public float Altitude;

        public float Climb;
        public override int ExpectedLength => 20;

        public VfrHud(MavPacket packet) : base(packet) 
        {
            Airspeed = BitConverter.ToSingle(FullPacket, 0);
            GroundSpeed = BitConverter.ToSingle(FullPacket, 4);
            Altitude = BitConverter.ToSingle(FullPacket, 8);
            Climb = BitConverter.ToSingle(FullPacket, 12);
            Heading = BitConverter.ToInt16(FullPacket, 16);
            Throttle = BitConverter.ToUInt16(FullPacket, 18);

        }

        public override void Print()
        {
            Console.WriteLine($"VFR HUD: Airspeed={Airspeed}, GroundSpeed={GroundSpeed}, Heading={Heading}, Throttle={Throttle}, Altitude={Altitude}, Climb={Climb}");
        }
    }
}
