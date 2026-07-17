namespace tLogViewer.Core.Models.Messages.RcChannelsMessage
{
    public class RcChannelsFactory
    {
        public static RcChannels ParseRcChannelsPacket(MavPacket packet)
        {
            if (RcChannelsV1.CanParse(packet))
                return new RcChannelsV1(packet);

            if(RcChannelsV2.CanParse(packet))
                return new RcChannelsV2(packet);

            return null;
        }
    }
}
