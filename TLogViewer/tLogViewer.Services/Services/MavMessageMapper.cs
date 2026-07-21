using tLogViewer.Core.Enums.Heartbeat;
using tLogViewer.Core.Enums.SysStatus;
using tLogViewer.Core.Models;
using tLogViewer.Core.Models.Messages;
using tLogViewer.Core.Models.Messages.RcChannelsMessage;
using tLogViewer.DTO.Messages;

namespace tLogViewer.Services.Services;

public static class MavMessageMapper
{
    public static MavMessageDto ToDto(MavLinkMessage message, ulong timeUs)
    {
        var messageId = ((int)message.Packet.MsgId).ToString();
        var timeUtc = TlogTime.ToUtcIso(timeUs);

        return message switch
        {
            Heartbeat m => Envelope("heartbeat", messageId, timeUtc, new HeartbeatData
            {
                CustomMode = m.CustomMode,
                AircraftType = m.Type.ToString(),
                Autopilot = m.Autopilot.ToString(),
                SystemStatus = m.SystemStatus.ToString(),
                Armed = m.BaseMode.HasFlag(MavModeFlag.SafetyArmed),
                BaseMode = (int)m.BaseMode,
                MavlinkVersion = m.MavlinkVersion
            }),
            SysStatus m => Envelope("sysStatus", messageId, timeUtc, new SysStatusData
            {
                Load = m.Load,
                BatteryVoltageV = m.BatteryVoltage / 1000.0,
                BatteryCurrentA = m.BatteryCurrent / 100.0,
                BatteryRemainingPct = m.BatteryRemaining,
                DropRateComm = m.DropRateComm,
                ErrorsComm = m.ErrorsComm,
                GpsPresent = m.SensorsPresent.HasFlag(MavSysSensor.GPS),
                GpsEnabled = m.SensorsEnabled.HasFlag(MavSysSensor.GPS),
                GpsHealthy = m.SensorsHealth.HasFlag(MavSysSensor.GPS)
            }),
            GpsRawInt m => Envelope("gpsRawInt", messageId, timeUtc, new GpsRawIntData
            {
                FixType = m.FixType.ToString(),
                LatitudeDeg = m.LatitudeDeg,
                LongitudeDeg = m.LongitudeDeg,
                AltitudeM = m.AltitudeM,
                GroundSpeedMS = m.GroundSpeedMS,
                CourseOverGroundDeg = m.CourseOverGroundDeg,
                SatellitesVisible = m.SatellitesVisible,
                Eph = m.Eph,
                Epv = m.Epv
            }),
            GpsStatus m => Envelope("gpsStatus", messageId, timeUtc, new GpsStatusData
            {
                SatellitesVisible = m.SatellitesVisible,
                SatellitePrn = m.SatellitePrn,
                SatelliteUsed = m.SatelliteUsed,
                SatelliteElevation = m.SatelliteElevation,
                SatelliteAzimuth = m.SatelliteAzimuth,
                SatelliteSnr = m.SatelliteSnr
            }),
            Attitude m => Envelope("attitude", messageId, timeUtc, new AttitudeData
            {
                TimeBootMs = m.TimeBootMs,
                RollDeg = m.RollDeg,
                PitchDeg = m.PitchDeg,
                YawDeg = m.YawDeg,
                RollSpeed = m.RollSpeed,
                PitchSpeed = m.PitchSpeed,
                YawSpeed = m.YawSpeed
            }),
            LocalPositionNed m => Envelope("localPositionNed", messageId, timeUtc, new LocalPositionNedData
            {
                TimeBootMs = m.TimeBootMs,
                X = m.X,
                Y = m.Y,
                Z = m.Z,
                Vx = m.Vx,
                Vy = m.Vy,
                Vz = m.Vz
            }),
            GlobalPositionInt m => Envelope("globalPositionInt", messageId, timeUtc, new GlobalPositionIntData
            {
                TimeBootMs = m.TimeBootMs,
                LatitudeDeg = m.LattitudeDeg,
                LongitudeDeg = m.LongitudeDeg,
                AslAltitudeM = m.AslAltitudeM,
                RelativeAltitudeM = m.RelativeAltitudeM,
                HorizontalVelocityMS = m.HorizontalVelocityMS,
                VerticalVelocityMS = m.VerticalVelocityMS,
                HeadingDeg = m.Heading / 100.0
            }),
            MissionCurrent m => Envelope("missionCurrent", messageId, timeUtc, new MissionCurrentData
            {
                Seq = m.Seq,
                Total = m.Total,
                MissionState = m.MissionState,
                MissionMode = m.MissionMode
            }),
            GpsGlobalOrigin m => Envelope("gpsGlobalOrigin", messageId, timeUtc, new GpsGlobalOriginData
            {
                LatitudeDeg = m.LatitudeDeg,
                LongitudeDeg = m.LongitudeDeg,
                AltitudeM = m.AltitudeM
            }),
            NavControllerOutput m => Envelope("navControllerOutput", messageId, timeUtc, new NavControllerOutputData
            {
                NavRoll = m.NavRoll,
                NavPitch = m.NavPitch,
                NavBearing = m.NavBearing,
                TargetBearing = m.TargetBearing,
                WpDistM = m.WpDist,
                AltErrorM = m.AltError,
                AspdErrorMS = m.AspdError,
                XtrackErrorM = m.XtrackError
            }),
            RcChannels m => Envelope("rcChannels", messageId, timeUtc, new RcChannelsData
            {
                TimeBootMs = m.TimeBootMs,
                ChannelCount = m.ChannelCount,
                Channels = m.Channels,
                Rssi = m.Rssi
            }),
            MissionItemInt m => Envelope("missionItemInt", messageId, timeUtc, new MissionItemIntData
            {
                Seq = m.Seq,
                Command = m.Command,
                Frame = m.Frame,
                Current = m.Current,
                Autocontinue = m.Autocontinue,
                LatitudeDeg = m.LatitudeDeg,
                LongitudeDeg = m.LongitudeDeg,
                AltitudeM = m.Z,
                Param1 = m.Param1,
                Param2 = m.Param2,
                Param3 = m.Param3,
                Param4 = m.Param4
            }),
            VfrHud m => Envelope("vfrHud", messageId, timeUtc, new VfrHudData
            {
                AirspeedMS = m.Airspeed,
                GroundSpeedMS = m.GroundSpeed,
                HeadingDeg = m.Heading,
                ThrottlePct = m.Throttle,
                AltitudeM = m.Altitude,
                ClimbMS = m.Climb
            }),
            PositionTargetGlobalInt m => Envelope("positionTargetGlobalInt", messageId, timeUtc, new PositionTargetGlobalIntData
            {
                TimeBootMs = m.TimeBootMs,
                LatitudeDeg = m.LatitudeDeg,
                LongitudeDeg = m.LongitudeDeg,
                AltitudeM = m.Alt,
                Vx = m.Vx,
                Vy = m.Vy,
                Vz = m.Vz,
                Yaw = m.Yaw,
                YawRate = m.YawRate,
                TypeMask = m.TypeMask,
                CoordinateFrame = m.CoordinateFrame
            }),
            HomePosition m => Envelope("homePosition", messageId, timeUtc, new HomePositionData
            {
                LatitudeDeg = m.LatitudeDeg,
                LongitudeDeg = m.LongitudeDeg,
                AltitudeM = m.AltitudeM,
                X = m.X,
                Y = m.Y,
                Z = m.Z
            }),
            _ => Envelope("unknown", messageId, timeUtc, new { })
        };
    }

    private static MavMessageDto Envelope(string type, string messageId, string timeUtc, object data) =>
        new()
        {
            Type = type,
            MessageId = messageId,
            TimeUtc = timeUtc,
            Data = data
        };
}
