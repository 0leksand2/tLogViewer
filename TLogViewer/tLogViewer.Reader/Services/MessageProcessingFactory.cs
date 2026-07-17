using System;
using System.Collections.Generic;
using System.Text;
using tLogViewer.Reader.Enums;
using tLogViewer.Reader.Enums.Heartbeat;
using tLogViewer.Reader.Models;
using tLogViewer.Reader.Models.Messages;

namespace tLogViewer.Reader.Services
{
    public class MessageProcessingFactory
    {
        public static MavLinkMessage ParseMessage(MavPacket packet)
        {
            switch (packet.MsgId)
            {
                case MavMessageTypeId.HEARTBEAT:
                    var message = new Heartbeat(packet);
                    return message.Type != AircraftType.Unknown ? message : null;
                //case MavMessageTypeId.SYS_STATUS:
                //    return new SysStatusMessage().Parse(packet);
                //case MavMessageTypeId.GPS_RAW_INT:
                //    return new GpsRawIntMessage().Parse(packet);
                case MavMessageTypeId.ATTITUDE:
                    return new Attitude(packet);
                //case MavMessageTypeId.RC_CHANNELS_RAW:
                //    return new RcChannelsRawMessage().Parse(packet);
                //case MavMessageTypeId.VFR_HUD:
                //    return new VfrHudMessage().Parse(packet);
                //case MavMessageTypeId.GLOBAL_POSITION_INT:
                //    return new GlobalPositionIntMessage().Parse(packet);
                default:
                    //throw new NotImplementedException($"Parsing for message type {packet.MsgId} is not implemented.");
                    return null;
            }
        }
    }
}
