namespace tLogViewer.Core.Models.Messages.RcChannelsMessage
{
    public class RcChannelsV1 : RcChannels
    {
        public RcChannelsV1(MavPacket packet) : base(packet)
        {
            TimeBootMs = BitConverter.ToUInt32(FullPacket, 0);

            ChannelCount = FullPacket[4];

            for (int i = 0; i < 18; i++)
            {
                Channels[i] =
                    BitConverter.ToUInt16(FullPacket, 5 + i * 2);
            }

            Rssi = FullPacket[41];
        }

        public static bool CanParse(MavPacket packet)
        {
            return packet.Payload.Length == 42 &&
                   packet.Payload[4] <= 18;
        }
    }
}
