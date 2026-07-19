namespace tLogViewer.Core.Models.Messages
{
    public class MissionItemInt : MavLinkMessage
    {
        public float Param1;
        public float Param2;
        public float Param3;
        public float Param4;
        public int X;
        public int Y;
        public float Z;
        public ushort Seq;
        public ushort Command;
        public byte TargetSystem;
        public byte TargetComponent;
        public byte Frame;
        public byte Current;
        public byte Autocontinue;
        public byte MissionType;

        public double LatitudeDeg;
        public double LongitudeDeg;

        public override int ExpectedLength => 38;

        public MissionItemInt(MavPacket packet) : base(packet)
        {
            Param1 = BitConverter.ToSingle(FullPacket, 0);
            Param2 = BitConverter.ToSingle(FullPacket, 4);
            Param3 = BitConverter.ToSingle(FullPacket, 8);
            Param4 = BitConverter.ToSingle(FullPacket, 12);
            X = BitConverter.ToInt32(FullPacket, 16);
            Y = BitConverter.ToInt32(FullPacket, 20);
            Z = BitConverter.ToSingle(FullPacket, 24);
            Seq = BitConverter.ToUInt16(FullPacket, 28);
            Command = BitConverter.ToUInt16(FullPacket, 30);
            TargetSystem = FullPacket[32];
            TargetComponent = FullPacket[33];
            Frame = FullPacket[34];
            Current = FullPacket[35];
            Autocontinue = FullPacket[36];
            MissionType = FullPacket[37];

            LatitudeDeg = X / 1e7;
            LongitudeDeg = Y / 1e7;
        }

        public override void Print()
        {
            Console.WriteLine($"MissionItemInt: Seq={Seq}, Command={Command}, Lat={LatitudeDeg:F7}, Lon={LongitudeDeg:F7}, Alt={Z:F1}");
        }
    }
}
