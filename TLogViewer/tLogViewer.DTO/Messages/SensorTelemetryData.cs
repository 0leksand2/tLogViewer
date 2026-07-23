namespace tLogViewer.DTO.Messages;

public sealed class ScaledImuData
{
    /// <summary>1 = SCALED_IMU, 2 = SCALED_IMU2, 3 = SCALED_IMU3.</summary>
    public int ImuIndex { get; init; }
    public double XAccG { get; init; }
    public double YAccG { get; init; }
    public double ZAccG { get; init; }
    public double XGyroRadS { get; init; }
    public double YGyroRadS { get; init; }
    public double ZGyroRadS { get; init; }
    public double XMagMgauss { get; init; }
    public double YMagMgauss { get; init; }
    public double ZMagMgauss { get; init; }
    public double? TemperatureC { get; init; }
}

public sealed class RawImuData
{
    public byte Id { get; init; }
    public short XAcc { get; init; }
    public short YAcc { get; init; }
    public short ZAcc { get; init; }
    public short XGyro { get; init; }
    public short YGyro { get; init; }
    public short ZGyro { get; init; }
    public short XMag { get; init; }
    public short YMag { get; init; }
    public short ZMag { get; init; }
    public double? TemperatureC { get; init; }
}

public sealed class HighresImuData
{
    public byte Id { get; init; }
    public double XAccMs2 { get; init; }
    public double YAccMs2 { get; init; }
    public double ZAccMs2 { get; init; }
    public double XGyroRadS { get; init; }
    public double YGyroRadS { get; init; }
    public double ZGyroRadS { get; init; }
    public double XMagGauss { get; init; }
    public double YMagGauss { get; init; }
    public double ZMagGauss { get; init; }
    public double AbsPressureHpa { get; init; }
    public double DiffPressureHpa { get; init; }
    public double PressureAlt { get; init; }
    public double TemperatureC { get; init; }
}

public sealed class VibrationData
{
    public double VibrationX { get; init; }
    public double VibrationY { get; init; }
    public double VibrationZ { get; init; }
    public uint Clipping0 { get; init; }
    public uint Clipping1 { get; init; }
    public uint Clipping2 { get; init; }
}

public sealed class EkfStatusReportData
{
    public double VelocityVariance { get; init; }
    public double PosHorizVariance { get; init; }
    public double PosVertVariance { get; init; }
    public double CompassVariance { get; init; }
    public double TerrainAltVariance { get; init; }
    public ushort Flags { get; init; }
    public double AirspeedVariance { get; init; }
}

public sealed class CompassmotStatusData
{
    public double CurrentA { get; init; }
    public double CompensationX { get; init; }
    public double CompensationY { get; init; }
    public double CompensationZ { get; init; }
    public double ThrottleDpct { get; init; }
    public double InterferencePct { get; init; }
}
