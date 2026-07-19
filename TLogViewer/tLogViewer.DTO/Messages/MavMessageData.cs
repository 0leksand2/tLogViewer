namespace tLogViewer.DTO.Messages;

public sealed class HeartbeatData
{
    public uint CustomMode { get; init; }
    public required string AircraftType { get; init; }
    public required string Autopilot { get; init; }
    public required string SystemStatus { get; init; }
    public bool Armed { get; init; }
    public int BaseMode { get; init; }
    public byte MavlinkVersion { get; init; }
}

public sealed class SysStatusData
{
    public ushort Load { get; init; }
    public double BatteryVoltageV { get; init; }
    public double BatteryCurrentA { get; init; }
    public sbyte BatteryRemainingPct { get; init; }
    public ushort DropRateComm { get; init; }
    public ushort ErrorsComm { get; init; }
    public bool GpsPresent { get; init; }
    public bool GpsEnabled { get; init; }
    public bool GpsHealthy { get; init; }
}

public sealed class GpsRawIntData
{
    public required string FixType { get; init; }
    public double LatitudeDeg { get; init; }
    public double LongitudeDeg { get; init; }
    public double AltitudeM { get; init; }
    public double GroundSpeedMS { get; init; }
    public double CourseOverGroundDeg { get; init; }
    public byte SatellitesVisible { get; init; }
    public ushort Eph { get; init; }
    public ushort Epv { get; init; }
}

public sealed class GpsStatusData
{
    public byte SatellitesVisible { get; init; }
    public required IReadOnlyList<byte> SatellitePrn { get; init; }
    public required IReadOnlyList<byte> SatelliteUsed { get; init; }
    public required IReadOnlyList<byte> SatelliteElevation { get; init; }
    public required IReadOnlyList<byte> SatelliteAzimuth { get; init; }
    public required IReadOnlyList<byte> SatelliteSnr { get; init; }
}

public sealed class AttitudeData
{
    public uint TimeBootMs { get; init; }
    public double RollDeg { get; init; }
    public double PitchDeg { get; init; }
    public double YawDeg { get; init; }
    public float RollSpeed { get; init; }
    public float PitchSpeed { get; init; }
    public float YawSpeed { get; init; }
}

public sealed class LocalPositionNedData
{
    public uint TimeBootMs { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
    public float Vx { get; init; }
    public float Vy { get; init; }
    public float Vz { get; init; }
}

public sealed class GlobalPositionIntData
{
    public uint TimeBootMs { get; init; }
    public double LatitudeDeg { get; init; }
    public double LongitudeDeg { get; init; }
    public double AslAltitudeM { get; init; }
    public double RelativeAltitudeM { get; init; }
    public double HorizontalVelocityMS { get; init; }
    public double VerticalVelocityMS { get; init; }
    public double HeadingDeg { get; init; }
}

public sealed class MissionCurrentData
{
    public ushort Seq { get; init; }
    public ushort Total { get; init; }
    public byte MissionState { get; init; }
    public byte MissionMode { get; init; }
}

public sealed class GpsGlobalOriginData
{
    public double LatitudeDeg { get; init; }
    public double LongitudeDeg { get; init; }
    public double AltitudeM { get; init; }
}

public sealed class NavControllerOutputData
{
    public float NavRoll { get; init; }
    public float NavPitch { get; init; }
    public short NavBearing { get; init; }
    public short TargetBearing { get; init; }
    public ushort WpDistM { get; init; }
    public float AltErrorM { get; init; }
    public float AspdErrorMS { get; init; }
    public float XtrackErrorM { get; init; }
}

public sealed class RcChannelsData
{
    public uint TimeBootMs { get; init; }
    public byte ChannelCount { get; init; }
    public required IReadOnlyList<double> Channels { get; init; }
    public byte Rssi { get; init; }
}

public sealed class MissionItemIntData
{
    public ushort Seq { get; init; }
    public ushort Command { get; init; }
    public byte Frame { get; init; }
    public byte Current { get; init; }
    public byte Autocontinue { get; init; }
    public double LatitudeDeg { get; init; }
    public double LongitudeDeg { get; init; }
    public float AltitudeM { get; init; }
    public float Param1 { get; init; }
    public float Param2 { get; init; }
    public float Param3 { get; init; }
    public float Param4 { get; init; }
}

public sealed class VfrHudData
{
    public float AirspeedMS { get; init; }
    public float GroundSpeedMS { get; init; }
    public short HeadingDeg { get; init; }
    public ushort ThrottlePct { get; init; }
    public float AltitudeM { get; init; }
    public float ClimbMS { get; init; }
}

public sealed class PositionTargetGlobalIntData
{
    public uint TimeBootMs { get; init; }
    public double LatitudeDeg { get; init; }
    public double LongitudeDeg { get; init; }
    public float AltitudeM { get; init; }
    public float Vx { get; init; }
    public float Vy { get; init; }
    public float Vz { get; init; }
    public float Yaw { get; init; }
    public float YawRate { get; init; }
    public ushort TypeMask { get; init; }
    public byte CoordinateFrame { get; init; }
}

public sealed class HomePositionData
{
    public double LatitudeDeg { get; init; }
    public double LongitudeDeg { get; init; }
    public double AltitudeM { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
}
