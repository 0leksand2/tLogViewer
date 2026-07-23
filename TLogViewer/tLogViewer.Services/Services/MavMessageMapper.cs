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
                Afx = m.Afx,
                Afy = m.Afy,
                Afz = m.Afz,
                Yaw = m.Yaw,
                YawDeg = m.YawDeg,
                YawRate = m.YawRate,
                YawRateDegS = m.YawRateDegS,
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
            Wind m => Envelope("wind", messageId, timeUtc, new WindData
            {
                DirectionDeg = m.DirectionDeg,
                SpeedMS = m.SpeedMS,
                SpeedZMS = m.SpeedZMS
            }),
            Radio m => Envelope("radio", messageId, timeUtc, new RadioData
            {
                Rssi = m.Rssi,
                RemRssi = m.RemRssi,
                TxBufPct = m.TxBufPct,
                Noise = m.Noise,
                RemNoise = m.RemNoise,
                RxErrors = m.RxErrors,
                Fixed = m.Fixed
            }),
            StatusText m => Envelope("statusText", messageId, timeUtc, new StatusTextData
            {
                Severity = (byte)m.Severity,
                Text = m.Text
            }),
            BatteryStatus m => Envelope("batteryStatus", messageId, timeUtc, new BatteryStatusData
            {
                Id = m.Id,
                BatteryRemainingPct = m.BatteryRemainingPct,
                VoltagesMv = m.VoltagesMv.ToArray(),
                VoltagesExtMv = m.VoltagesExtMv.ToArray(),
                CurrentBatteryA = m.CurrentBatteryCA < 0 ? null : m.CurrentBatteryCA / 100.0,
                TemperatureC = m.TemperatureCdegC == short.MaxValue
                    ? null
                    : m.TemperatureCdegC / 100.0,
                CurrentConsumedMah = m.CurrentConsumedMah < 0 ? null : m.CurrentConsumedMah
            }),
            ScaledImu m => Envelope("scaledImu", messageId, timeUtc, new ScaledImuData
            {
                ImuIndex = ScaledImuIndex(messageId),
                XAccG = m.XAccMg / 1000.0,
                YAccG = m.YAccMg / 1000.0,
                ZAccG = m.ZAccMg / 1000.0,
                XGyroRadS = m.XGyroMradS / 1000.0,
                YGyroRadS = m.YGyroMradS / 1000.0,
                ZGyroRadS = m.ZGyroMradS / 1000.0,
                XMagMgauss = m.XMagMgauss,
                YMagMgauss = m.YMagMgauss,
                ZMagMgauss = m.ZMagMgauss,
                TemperatureC = m.TemperatureCdegC is { } t ? t / 100.0 : null
            }),
            RawImu m => Envelope("rawImu", messageId, timeUtc, new RawImuData
            {
                Id = m.Id,
                XAcc = m.XAcc,
                YAcc = m.YAcc,
                ZAcc = m.ZAcc,
                XGyro = m.XGyro,
                YGyro = m.YGyro,
                ZGyro = m.ZGyro,
                XMag = m.XMag,
                YMag = m.YMag,
                ZMag = m.ZMag,
                TemperatureC = m.TemperatureCdegC is { } t ? t / 100.0 : null
            }),
            HighresImu m => Envelope("highresImu", messageId, timeUtc, new HighresImuData
            {
                Id = m.Id,
                XAccMs2 = m.XAccMs2,
                YAccMs2 = m.YAccMs2,
                ZAccMs2 = m.ZAccMs2,
                XGyroRadS = m.XGyroRadS,
                YGyroRadS = m.YGyroRadS,
                ZGyroRadS = m.ZGyroRadS,
                XMagGauss = m.XMagGauss,
                YMagGauss = m.YMagGauss,
                ZMagGauss = m.ZMagGauss,
                AbsPressureHpa = m.AbsPressureHpa,
                DiffPressureHpa = m.DiffPressureHpa,
                PressureAlt = m.PressureAlt,
                TemperatureC = m.TemperatureC
            }),
            Vibration m => Envelope("vibration", messageId, timeUtc, new VibrationData
            {
                VibrationX = m.VibrationX,
                VibrationY = m.VibrationY,
                VibrationZ = m.VibrationZ,
                Clipping0 = m.Clipping0,
                Clipping1 = m.Clipping1,
                Clipping2 = m.Clipping2
            }),
            EkfStatusReport m => Envelope("ekfStatusReport", messageId, timeUtc, new EkfStatusReportData
            {
                VelocityVariance = m.VelocityVariance,
                PosHorizVariance = m.PosHorizVariance,
                PosVertVariance = m.PosVertVariance,
                CompassVariance = m.CompassVariance,
                TerrainAltVariance = m.TerrainAltVariance,
                Flags = m.Flags,
                AirspeedVariance = m.AirspeedVariance
            }),
            CompassmotStatus m => Envelope("compassmotStatus", messageId, timeUtc, new CompassmotStatusData
            {
                CurrentA = m.CurrentA,
                CompensationX = m.CompensationX,
                CompensationY = m.CompensationY,
                CompensationZ = m.CompensationZ,
                ThrottleDpct = m.ThrottleDpct,
                InterferencePct = m.InterferencePct
            }),
            _ => Envelope("unknown", messageId, timeUtc, new { })
        };
    }

    private static int ScaledImuIndex(string messageId) => messageId switch
    {
        "116" => 2,
        "129" => 3,
        _ => 1
    };

    private static MavMessageDto Envelope(string type, string messageId, string timeUtc, object data) =>
        new()
        {
            Type = type,
            MessageId = messageId,
            TimeUtc = timeUtc,
            Data = data
        };
}
