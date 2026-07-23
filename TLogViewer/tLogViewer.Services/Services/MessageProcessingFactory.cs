using tLogViewer.Core.Enums;
using tLogViewer.Core.Enums.Heartbeat;
using tLogViewer.Core.Models;
using tLogViewer.Core.Models.Messages;
using tLogViewer.Core.Models.Messages.RcChannelsMessage;

namespace tLogViewer.Services.Services;

public static class MessageProcessingFactory
{
    public static MavLinkMessage? ParseMessage(MavPacket packet)
    {
        // Payload longer than the known max means a desynced / corrupt frame.
        if (packet.Payload.Length > MaxPayloadLength(packet.MsgId))
            return null;

        switch (packet.MsgId)
        {
            case MavMessageTypeId.HEARTBEAT:
                var message = new Heartbeat(packet);
                return message.Type != AircraftType.Unknown ? message : null;
            case MavMessageTypeId.SYS_STATUS:
                return new SysStatus(packet);
            case MavMessageTypeId.GPS_RAW_INT:
                return new GpsRawInt(packet);
            case MavMessageTypeId.GPS_STATUS:
                return new GpsStatus(packet);
            case MavMessageTypeId.ATTITUDE:
                return new Attitude(packet);
            case MavMessageTypeId.LOCAL_POSITION_NED:
                return new LocalPositionNed(packet);
            case MavMessageTypeId.GLOBAL_POSITION_INT:
                return new GlobalPositionInt(packet);
            case MavMessageTypeId.MISSION_CURRENT:
                return new MissionCurrent(packet);
            case MavMessageTypeId.GPS_GLOBAL_ORIGIN:
                return new GpsGlobalOrigin(packet);
            case MavMessageTypeId.NAV_CONTROLLER_OUTPUT:
                return new NavControllerOutput(packet);
            case MavMessageTypeId.RC_CHANNELS:
                return RcChannelsFactory.ParseRcChannelsPacket(packet);
            case MavMessageTypeId.MISSION_ITEM_INT:
                return new MissionItemInt(packet);
            case MavMessageTypeId.VFR_HUD:
                return new VfrHud(packet);
            case MavMessageTypeId.POSITION_TARGET_GLOBAL_INT:
                return new PositionTargetGlobalInt(packet);
            case MavMessageTypeId.RADIO_STATUS:
            case MavMessageTypeId.RADIO:
                return new Radio(packet);
            case MavMessageTypeId.WIND:
                return new Wind(packet);
            case MavMessageTypeId.HOME_POSITION:
                return new HomePosition(packet);
            case MavMessageTypeId.STATUSTEXT:
                return new StatusText(packet);
            default:
                return null;
        }
    }

    private static int MaxPayloadLength(MavMessageTypeId msgId) => msgId switch
    {
        MavMessageTypeId.HEARTBEAT => 9,
        MavMessageTypeId.SYS_STATUS => 31,
        MavMessageTypeId.GPS_RAW_INT => 52,
        MavMessageTypeId.GPS_STATUS => 101,
        MavMessageTypeId.ATTITUDE => 28,
        MavMessageTypeId.LOCAL_POSITION_NED => 28,
        MavMessageTypeId.GLOBAL_POSITION_INT => 28,
        MavMessageTypeId.MISSION_CURRENT => 18,
        MavMessageTypeId.GPS_GLOBAL_ORIGIN => 20,
        MavMessageTypeId.NAV_CONTROLLER_OUTPUT => 26,
        MavMessageTypeId.RC_CHANNELS => 42,
        MavMessageTypeId.MISSION_ITEM_INT => 38,
        MavMessageTypeId.VFR_HUD => 20,
        MavMessageTypeId.POSITION_TARGET_GLOBAL_INT => 51,
        MavMessageTypeId.RADIO_STATUS => 9,
        MavMessageTypeId.RADIO => 9,
        MavMessageTypeId.WIND => 12,
        MavMessageTypeId.HOME_POSITION => 60,
        // Classic 51; MAVLink2 may include id + chunk_seq (54).
        MavMessageTypeId.STATUSTEXT => 54,
        _ => int.MaxValue
    };
}
