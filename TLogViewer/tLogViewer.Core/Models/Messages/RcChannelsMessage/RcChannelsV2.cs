namespace tLogViewer.Core.Models.Messages.RcChannelsMessage
{
    public class RcChannelsV2 : RcChannels
    {
        public RcChannelsV2(MavPacket packet) : base(packet)
        {
            TimeBootMs = BitConverter.ToUInt32(FullPacket, 0);

            for (int i = 0; i < 18; i++)
            {
                Channels[i] =
                    BitConverter.ToUInt16(FullPacket, 4 + i * 2);
            }

            ChannelCount = FullPacket[40];

            Rssi = FullPacket[41];
        }

        public static bool CanParse(MavPacket packet)
        {
            return packet.Payload.Length == 42 &&
                   packet.Payload[40] <= 18;
        }
    }
}
