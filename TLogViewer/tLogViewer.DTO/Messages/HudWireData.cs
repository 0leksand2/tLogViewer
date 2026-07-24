namespace tLogViewer.DTO.Messages;

public sealed class ScaledPressureData
{
    public int SensorIndex { get; init; }
    public double PressAbsHpa { get; init; }
    public double PressDiffHpa { get; init; }
    public double TemperatureC { get; init; }
    public double? TemperaturePressDiffC { get; init; }
}

public sealed class ServoOutputRawData
{
    public byte Port { get; init; }
    public required ushort[] ServoRaw { get; init; }
}

public sealed class OpticalFlowData
{
    public float FlowCompMX { get; init; }
    public float FlowCompMY { get; init; }
    public short FlowX { get; init; }
    public short FlowY { get; init; }
    public byte Quality { get; init; }
}

public sealed class Gps2RawData
{
    public required string FixType { get; init; }
    public double LatitudeDeg { get; init; }
    public double LongitudeDeg { get; init; }
    public double AltitudeM { get; init; }
    public double GroundSpeedMS { get; init; }
    public double CourseOverGroundDeg { get; init; }
    public byte SatellitesVisible { get; init; }
    public ushort Eph { get; init; }
    public uint? HAcc { get; init; }
    public uint? VAcc { get; init; }
    public uint? VelAcc { get; init; }
    public uint? HdgAcc { get; init; }
    public ushort? YawCdeg { get; init; }
}

public sealed class PowerStatusData
{
    public double BoardVoltageV { get; init; }
    public double ServoVoltageV { get; init; }
    public ushort VoltageFlag { get; init; }
}

public sealed class DistanceSensorData
{
    public byte Id { get; init; }
    public ushort CurrentDistanceCm { get; init; }
}

public sealed class TerrainReportData
{
    public float TerrainHeightM { get; init; }
    public float CurrentHeightM { get; init; }
    public ushort Pending { get; init; }
    public ushort Loaded { get; init; }
    public ushort Spacing { get; init; }
}

public sealed class MemInfoData
{
    public ushort BrkLevel { get; init; }
    public uint Freemem { get; init; }
}

public sealed class MountStatusData
{
    public double PointingADeg { get; init; }
    public double PointingBDeg { get; init; }
    public double PointingCDeg { get; init; }
}

public sealed class FenceStatusData
{
    public byte BreachStatus { get; init; }
    public ushort BreachCount { get; init; }
    public byte BreachType { get; init; }
}

public sealed class HwStatusData
{
    public double HwVoltageV { get; init; }
    public byte I2cErrors { get; init; }
}

public sealed class RangefinderData
{
    public float DistanceM { get; init; }
    public float VoltageV { get; init; }
}

public sealed class AirspeedAutocalData
{
    public float Ratio { get; init; }
}

public sealed class Ahrs2Data
{
    public double RollDeg { get; init; }
    public double PitchDeg { get; init; }
    public double YawDeg { get; init; }
    public float AltitudeM { get; init; }
    public double LatitudeDeg { get; init; }
    public double LongitudeDeg { get; init; }
}

public sealed class PidTuningData
{
    public byte Axis { get; init; }
    public float Desired { get; init; }
    public float Achieved { get; init; }
    public float Ff { get; init; }
    public float P { get; init; }
    public float I { get; init; }
    public float D { get; init; }
    public float? SRate { get; init; }
}

public sealed class RpmData
{
    public float Rpm1 { get; init; }
    public float Rpm2 { get; init; }
}

public sealed class EfiStatusData
{
    public byte Health { get; init; }
    public float Rpm { get; init; }
    public float FuelConsumedCm3 { get; init; }
    public float FuelFlowCm3Min { get; init; }
    public float EngineLoadPct { get; init; }
    public float BarometricPressureKpa { get; init; }
    public float IntakeManifoldTemperatureC { get; init; }
    public float CylinderHeadTemperatureC { get; init; }
    public float ExhaustGasTemperatureC { get; init; }
    public float FuelPressureKpa { get; init; }
}

public sealed class ExtendedSysStateData
{
    public byte VtolState { get; init; }
    public byte LandedState { get; init; }
}

public sealed class AutopilotVersionData
{
    public ulong Capabilities { get; init; }
    public ulong Uid { get; init; }
}

public sealed class GeneratorStatusData
{
    public ulong Status { get; init; }
    public ushort GeneratorSpeed { get; init; }
    public float BatteryCurrentA { get; init; }
    public float BusVoltageV { get; init; }
    public uint RuntimeSec { get; init; }
    public int TimeUntilMaintenanceSec { get; init; }
}

public sealed class EscTelemetryData
{
    public int FirstEscIndex { get; init; }
    public required byte[] TemperatureDegC { get; init; }
    public required ushort[] VoltageCv { get; init; }
    public required ushort[] CurrentCa { get; init; }
    public required ushort[] RpmErpm { get; init; }
}

public sealed class EscInfoData
{
    /// <summary>0-based first ESC index in this message.</summary>
    public byte Index { get; init; }
    public byte Count { get; init; }
    /// <summary>cdegC; INT16_MAX means not supplied.</summary>
    public required short[] TemperatureCdegC { get; init; }
}

public sealed class EscStatusData
{
    /// <summary>0-based first ESC index in this message.</summary>
    public byte Index { get; init; }
    public required int[] Rpm { get; init; }
    public required float[] VoltageV { get; init; }
    public required float[] CurrentA { get; init; }
}

public sealed class McuStatusData
{
    public double TemperatureC { get; init; }
    public double VoltageV { get; init; }
    public double VoltageMinV { get; init; }
    public double VoltageMaxV { get; init; }
}

public sealed class HygrometerSensorData
{
    public byte Id { get; init; }
    public double TemperatureC { get; init; }
    public double HumidityPct { get; init; }
}

public sealed class AoaSsaData
{
    public float AoaDeg { get; init; }
    public float SsaDeg { get; init; }
}

public sealed class UavionixAdsbOutStatusData
{
    public ushort Squawk { get; init; }
    public byte State { get; init; }
    public byte NicNacp { get; init; }
    public byte BoardTempC { get; init; }
    public byte Fault { get; init; }
}
