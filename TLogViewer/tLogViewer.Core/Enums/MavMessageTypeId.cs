namespace tLogViewer.Core.Enums
{
    public enum MavMessageTypeId
    {
        HEARTBEAT = 0,
        SYS_STATUS = 1,
        GPS_RAW_INT = 24,
        GPS_STATUS = 25,
        ATTITUDE = 30,
        LOCAL_POSITION_NED = 32,
        GLOBAL_POSITION_INT = 33,
        MISSION_CURRENT = 42,
        GPS_GLOBAL_ORIGIN = 49,
        NAV_CONTROLLER_OUTPUT = 62,
        RC_CHANNELS = 65,
        MISSION_ITEM_INT = 73,
        VFR_HUD = 74,
        POSITION_TARGET_GLOBAL_INT = 87,
        RADIO_STATUS = 109,
        BATTERY_STATUS = 147,
        RADIO = 166,
        WIND = 168,
        HOME_POSITION = 242,
        STATUSTEXT = 253,
        /// <summary>Synthetic derived telemetry (not a wire MAVLink id).</summary>
        DERIVED = 998
    }
}
