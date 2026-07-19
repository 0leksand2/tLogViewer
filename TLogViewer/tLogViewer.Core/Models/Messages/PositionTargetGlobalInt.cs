namespace tLogViewer.Core.Models.Messages
{
    public class PositionTargetGlobalInt : MavLinkMessage
    {
        public uint TimeBootMs;
        public int LatInt;
        public int LonInt;
        public float Alt;
        public float Vx;
        public float Vy;
        public float Vz;
        public float Afx;
        public float Afy;
        public float Afz;
        public float Yaw;
        public float YawRate;
        public ushort TypeMask;
        public byte CoordinateFrame;

        public double LatitudeDeg;
        public double LongitudeDeg;

        public override int ExpectedLength => 51;

        public PositionTargetGlobalInt(MavPacket packet) : base(packet)
        {
            TimeBootMs = BitConverter.ToUInt32(FullPacket, 0);
            LatInt = BitConverter.ToInt32(FullPacket, 4);
            LonInt = BitConverter.ToInt32(FullPacket, 8);
            Alt = BitConverter.ToSingle(FullPacket, 12);
            Vx = BitConverter.ToSingle(FullPacket, 16);
            Vy = BitConverter.ToSingle(FullPacket, 20);
            Vz = BitConverter.ToSingle(FullPacket, 24);
            Afx = BitConverter.ToSingle(FullPacket, 28);
            Afy = BitConverter.ToSingle(FullPacket, 32);
            Afz = BitConverter.ToSingle(FullPacket, 36);
            Yaw = BitConverter.ToSingle(FullPacket, 40);
            YawRate = BitConverter.ToSingle(FullPacket, 44);
            TypeMask = BitConverter.ToUInt16(FullPacket, 48);
            CoordinateFrame = FullPacket[50];

            LatitudeDeg = LatInt / 1e7;
            LongitudeDeg = LonInt / 1e7;
        }

        public override void Print()
        {
            Console.WriteLine($"PositionTargetGlobalInt: Lat={LatitudeDeg:F7}, Lon={LongitudeDeg:F7}, Alt={Alt:F1}, Frame={CoordinateFrame}");
        }
    }
}
