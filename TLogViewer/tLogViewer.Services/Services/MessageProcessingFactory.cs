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
            case MavMessageTypeId.SCALED_IMU:
            case MavMessageTypeId.SCALED_IMU2:
            case MavMessageTypeId.SCALED_IMU3:
                return new ScaledImu(packet);
            case MavMessageTypeId.RAW_IMU:
                return new RawImu(packet);
            case MavMessageTypeId.SCALED_PRESSURE:
            case MavMessageTypeId.SCALED_PRESSURE2:
                return new ScaledPressure(packet);
            case MavMessageTypeId.ATTITUDE:
                return new Attitude(packet);
            case MavMessageTypeId.LOCAL_POSITION_NED:
                return new LocalPositionNed(packet);
            case MavMessageTypeId.GLOBAL_POSITION_INT:
                return new GlobalPositionInt(packet);
            case MavMessageTypeId.SERVO_OUTPUT_RAW:
                return new ServoOutputRaw(packet);
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
            case MavMessageTypeId.OPTICAL_FLOW:
                return new OpticalFlow(packet);
            case MavMessageTypeId.HIGHRES_IMU:
                return new HighresImu(packet);
            case MavMessageTypeId.RADIO_STATUS:
            case MavMessageTypeId.RADIO:
                return new Radio(packet);
            case MavMessageTypeId.GPS2_RAW:
                return new Gps2Raw(packet);
            case MavMessageTypeId.POWER_STATUS:
                return new PowerStatus(packet);
            case MavMessageTypeId.DISTANCE_SENSOR:
                return new DistanceSensor(packet);
            case MavMessageTypeId.TERRAIN_REPORT:
                return new TerrainReport(packet);
            case MavMessageTypeId.BATTERY_STATUS:
                return new BatteryStatus(packet);
            case MavMessageTypeId.AUTOPILOT_VERSION:
                return new AutopilotVersion(packet);
            case MavMessageTypeId.MEMINFO:
                return new MemInfo(packet);
            case MavMessageTypeId.MOUNT_STATUS:
                return new MountStatus(packet);
            case MavMessageTypeId.FENCE_STATUS:
                return new FenceStatus(packet);
            case MavMessageTypeId.HWSTATUS:
                return new HwStatus(packet);
            case MavMessageTypeId.WIND:
                return new Wind(packet);
            case MavMessageTypeId.RANGEFINDER:
                return new Rangefinder(packet);
            case MavMessageTypeId.AIRSPEED_AUTOCAL:
                return new AirspeedAutocal(packet);
            case MavMessageTypeId.COMPASSMOT_STATUS:
                return new CompassmotStatus(packet);
            case MavMessageTypeId.AHRS2:
                return new Ahrs2(packet);
            case MavMessageTypeId.EKF_STATUS_REPORT:
                return new EkfStatusReport(packet);
            case MavMessageTypeId.PID_TUNING:
                return new PidTuning(packet);
            case MavMessageTypeId.EFI_STATUS:
                return new EfiStatus(packet);
            case MavMessageTypeId.RPM:
                return new Rpm(packet);
            case MavMessageTypeId.VIBRATION:
                return new Vibration(packet);
            case MavMessageTypeId.HOME_POSITION:
                return new HomePosition(packet);
            case MavMessageTypeId.EXTENDED_SYS_STATE:
                return new ExtendedSysState(packet);
            case MavMessageTypeId.STATUSTEXT:
                return new StatusText(packet);
            case MavMessageTypeId.GPS_INPUT:
                return new GpsInput(packet);
            case MavMessageTypeId.GENERATOR_STATUS:
                return new GeneratorStatus(packet);
            case MavMessageTypeId.ESC_INFO:
                return new EscInfo(packet);
            case MavMessageTypeId.ESC_STATUS:
                return new EscStatus(packet);
            case MavMessageTypeId.UAVIONIX_ADSB_OUT_STATUS:
                return new UavionixAdsbOutStatus(packet);
            case MavMessageTypeId.AOA_SSA:
                return new AoaSsa(packet);
            case MavMessageTypeId.ESC_TELEMETRY_1_TO_4:
                return CreateEscTelemetry(packet, 1);
            case MavMessageTypeId.ESC_TELEMETRY_5_TO_8:
                return CreateEscTelemetry(packet, 5);
            case MavMessageTypeId.ESC_TELEMETRY_9_TO_12:
                return CreateEscTelemetry(packet, 9);
            case MavMessageTypeId.ESC_TELEMETRY_13_TO_16:
                return CreateEscTelemetry(packet, 13);
            case MavMessageTypeId.MCU_STATUS:
                return new McuStatus(packet);
            case MavMessageTypeId.HYGROMETER_SENSOR:
                return new HygrometerSensor(packet);
            default:
                return null;
        }
    }

    private static EscTelemetry CreateEscTelemetry(MavPacket packet, int firstEscIndex)
    {
        var esc = new EscTelemetry(packet) { FirstEscIndex = firstEscIndex };
        return esc;
    }

    private static int MaxPayloadLength(MavMessageTypeId msgId) => msgId switch
    {
        MavMessageTypeId.HEARTBEAT => 9,
        MavMessageTypeId.SYS_STATUS => 31,
        MavMessageTypeId.GPS_RAW_INT => 52,
        MavMessageTypeId.GPS_STATUS => 101,
        MavMessageTypeId.SCALED_IMU => 24,
        MavMessageTypeId.SCALED_IMU2 => 24,
        MavMessageTypeId.SCALED_IMU3 => 24,
        MavMessageTypeId.RAW_IMU => 29,
        MavMessageTypeId.SCALED_PRESSURE => 16,
        MavMessageTypeId.SCALED_PRESSURE2 => 16,
        MavMessageTypeId.ATTITUDE => 28,
        MavMessageTypeId.LOCAL_POSITION_NED => 28,
        MavMessageTypeId.GLOBAL_POSITION_INT => 28,
        MavMessageTypeId.SERVO_OUTPUT_RAW => 37,
        MavMessageTypeId.MISSION_CURRENT => 18,
        MavMessageTypeId.GPS_GLOBAL_ORIGIN => 20,
        MavMessageTypeId.NAV_CONTROLLER_OUTPUT => 26,
        MavMessageTypeId.RC_CHANNELS => 42,
        MavMessageTypeId.MISSION_ITEM_INT => 38,
        MavMessageTypeId.VFR_HUD => 20,
        MavMessageTypeId.POSITION_TARGET_GLOBAL_INT => 51,
        MavMessageTypeId.OPTICAL_FLOW => 34,
        MavMessageTypeId.HIGHRES_IMU => 63,
        MavMessageTypeId.RADIO_STATUS => 9,
        MavMessageTypeId.RADIO => 9,
        MavMessageTypeId.GPS2_RAW => 57,
        MavMessageTypeId.POWER_STATUS => 6,
        MavMessageTypeId.DISTANCE_SENSOR => 14,
        MavMessageTypeId.TERRAIN_REPORT => 22,
        MavMessageTypeId.BATTERY_STATUS => 54,
        MavMessageTypeId.AUTOPILOT_VERSION => 60,
        MavMessageTypeId.MEMINFO => 8,
        MavMessageTypeId.MOUNT_STATUS => 14,
        MavMessageTypeId.FENCE_STATUS => 8,
        MavMessageTypeId.HWSTATUS => 3,
        MavMessageTypeId.WIND => 12,
        MavMessageTypeId.RANGEFINDER => 8,
        MavMessageTypeId.AIRSPEED_AUTOCAL => 48,
        MavMessageTypeId.COMPASSMOT_STATUS => 20,
        MavMessageTypeId.AHRS2 => 24,
        MavMessageTypeId.EKF_STATUS_REPORT => 26,
        MavMessageTypeId.PID_TUNING => 33,
        MavMessageTypeId.EFI_STATUS => 73,
        MavMessageTypeId.RPM => 8,
        MavMessageTypeId.VIBRATION => 32,
        MavMessageTypeId.HOME_POSITION => 60,
        MavMessageTypeId.EXTENDED_SYS_STATE => 2,
        MavMessageTypeId.STATUSTEXT => 54,
        MavMessageTypeId.GPS_INPUT => 65,
        MavMessageTypeId.GENERATOR_STATUS => 42,
        MavMessageTypeId.ESC_INFO => 46,
        MavMessageTypeId.ESC_STATUS => 57,
        MavMessageTypeId.UAVIONIX_ADSB_OUT_STATUS => 14,
        MavMessageTypeId.AOA_SSA => 16,
        MavMessageTypeId.ESC_TELEMETRY_1_TO_4 => 44,
        MavMessageTypeId.ESC_TELEMETRY_5_TO_8 => 44,
        MavMessageTypeId.ESC_TELEMETRY_9_TO_12 => 44,
        MavMessageTypeId.ESC_TELEMETRY_13_TO_16 => 44,
        MavMessageTypeId.MCU_STATUS => 9,
        MavMessageTypeId.HYGROMETER_SENSOR => 5,
        _ => int.MaxValue
    };
}
