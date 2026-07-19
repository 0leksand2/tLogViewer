namespace tLogViewer.Core.Models.Messages
{
    public class LocalPositionNed : MavLinkMessage
    {
        public uint TimeBootMs;
        public float X;
        public float Y;
        public float Z;
        public float Vx;
        public float Vy;
        public float Vz;

        public override int ExpectedLength => 28;

        public LocalPositionNed(MavPacket packet) : base(packet)
        {
            TimeBootMs = BitConverter.ToUInt32(FullPacket, 0);
            X = BitConverter.ToSingle(FullPacket, 4);
            Y = BitConverter.ToSingle(FullPacket, 8);
            Z = BitConverter.ToSingle(FullPacket, 12);
            Vx = BitConverter.ToSingle(FullPacket, 16);
            Vy = BitConverter.ToSingle(FullPacket, 20);
            Vz = BitConverter.ToSingle(FullPacket, 24);
        }

        public override void Print()
        {
            Console.WriteLine($"LocalPositionNed: X={X:F2}, Y={Y:F2}, Z={Z:F2}, Vx={Vx:F2}, Vy={Vy:F2}, Vz={Vz:F2}");
        }
    }
}
