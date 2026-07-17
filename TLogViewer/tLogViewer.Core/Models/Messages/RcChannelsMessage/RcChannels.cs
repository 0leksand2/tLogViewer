namespace tLogViewer.Core.Models.Messages.RcChannelsMessage
{
    public class RcChannels : MavLinkMessage
    {
        public override int ExpectedLength => 42;

        public uint TimeBootMs;

        public byte ChannelCount;

        public double[] Channels = new double[18];

        public byte Rssi;

        public RcChannels(MavPacket packet) : base(packet)
        {
            TimeBootMs = BitConverter.ToUInt32(FullPacket, 0);

            ChannelCount = FullPacket[4];

            for (int i = 0; i < 18; i++)
            {
                Channels[i] = (BitConverter.ToUInt16(FullPacket, 5 + i * 2) - 1500) / 500; 
            }

            Rssi = FullPacket[41];
        }

        public override void Print()
        {
            Console.WriteLine($"RC Channels Message: Throttle={Channels[2]}");
        }
    }
}
