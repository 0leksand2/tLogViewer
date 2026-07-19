namespace tLogViewer.Core.Models.Messages
{
    public class MissionCurrent : MavLinkMessage
    {
        public ushort Seq;
        public ushort Total;
        public byte MissionState;
        public byte MissionMode;
        public uint MissionId;
        public uint FenceId;
        public uint RallyPointsId;

        public override int ExpectedLength => 18;

        public MissionCurrent(MavPacket packet) : base(packet)
        {
            Seq = BitConverter.ToUInt16(FullPacket, 0);
            Total = BitConverter.ToUInt16(FullPacket, 2);
            MissionState = FullPacket[4];
            MissionMode = FullPacket[5];
            MissionId = BitConverter.ToUInt32(FullPacket, 6);
            FenceId = BitConverter.ToUInt32(FullPacket, 10);
            RallyPointsId = BitConverter.ToUInt32(FullPacket, 14);
        }

        public override void Print()
        {
            Console.WriteLine($"MissionCurrent: Seq={Seq}, Total={Total}, State={MissionState}, Mode={MissionMode}");
        }
    }
}
