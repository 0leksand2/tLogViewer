namespace tLogViewer.Core.Models.Messages
{
    public class NavControllerOutput : MavLinkMessage
    {
        public float NavRoll;
        public float NavPitch;
        public float AltError;
        public float AspdError;
        public float XtrackError;
        public short NavBearing;
        public short TargetBearing;
        public ushort WpDist;

        public override int ExpectedLength => 26;

        public NavControllerOutput(MavPacket packet) : base(packet)
        {
            NavRoll = BitConverter.ToSingle(FullPacket, 0);
            NavPitch = BitConverter.ToSingle(FullPacket, 4);
            AltError = BitConverter.ToSingle(FullPacket, 8);
            AspdError = BitConverter.ToSingle(FullPacket, 12);
            XtrackError = BitConverter.ToSingle(FullPacket, 16);
            NavBearing = BitConverter.ToInt16(FullPacket, 20);
            TargetBearing = BitConverter.ToInt16(FullPacket, 22);
            WpDist = BitConverter.ToUInt16(FullPacket, 24);
        }

        public override void Print()
        {
            Console.WriteLine($"NavControllerOutput: WpDist={WpDist}m, TargetBearing={TargetBearing}, AltError={AltError:F1}, XtrackError={XtrackError:F1}");
        }
    }
}
