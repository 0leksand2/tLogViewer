using tLogViewer.Core.Models;
using tLogViewer.DTO.Messages;

namespace tLogViewer.Services.Services;

/// <summary>
/// Maps IMU / EKF / vibration / compassmot messages onto Mission Planner 999_* keys.
/// </summary>
public static class SensorFieldsEnricher
{
    private const double GravityMs2 = 9.80665;

    /// <summary>Writes known sensor messages into Mission Planner telemetry keys.</summary>
    public static bool TryPush(Dictionary<string, object> atMs, MavMessageDto message)
    {
        switch (message.Data)
        {
            case ScaledImuData scaled:
                WriteScaledImu(atMs, scaled);
                return true;
            case RawImuData raw:
                WriteRawImu(atMs, raw);
                return true;
            case HighresImuData highres:
                WriteHighresImu(atMs, highres);
                return true;
            case VibrationData vibe:
                WriteVibration(atMs, vibe);
                return true;
            case EkfStatusReportData ekf:
                WriteEkf(atMs, ekf);
                return true;
            case CompassmotStatusData compassmot:
                WriteCompassmot(atMs, compassmot);
                return true;
            default:
                return false;
        }
    }

    private static void WriteScaledImu(Dictionary<string, object> atMs, ScaledImuData data)
    {
        var keys = ImuKeys.ForIndex(data.ImuIndex);
        atMs[keys.Ax] = data.XAccG;
        atMs[keys.Ay] = data.YAccG;
        atMs[keys.Az] = data.ZAccG;
        atMs[keys.Gx] = data.XGyroRadS;
        atMs[keys.Gy] = data.YGyroRadS;
        atMs[keys.Gz] = data.ZGyroRadS;
        atMs[keys.Mx] = data.XMagMgauss;
        atMs[keys.My] = data.YMagMgauss;
        atMs[keys.Mz] = data.ZMagMgauss;
        atMs[keys.AccelSq] = MagnitudeSq(data.XAccG, data.YAccG, data.ZAccG);
        atMs[keys.GyroSq] = MagnitudeSq(data.XGyroRadS, data.YGyroRadS, data.ZGyroRadS);
        atMs[keys.MagField] = Math.Sqrt(
            MagnitudeSq(data.XMagMgauss, data.YMagMgauss, data.ZMagMgauss));
        if (data.TemperatureC is { } temp)
        {
            atMs[keys.Temp] = temp;
        }
    }

    private static void WriteRawImu(Dictionary<string, object> atMs, RawImuData data)
    {
        // RAW_IMU id 0/1/2 → IMU1/2/3. Prefer not overwriting SCALED values already present.
        var index = data.Id is >= 0 and <= 2 ? data.Id + 1 : 1;
        var keys = ImuKeys.ForIndex(index);
        WriteIfAbsent(atMs, keys.Ax, data.XAcc);
        WriteIfAbsent(atMs, keys.Ay, data.YAcc);
        WriteIfAbsent(atMs, keys.Az, data.ZAcc);
        WriteIfAbsent(atMs, keys.Gx, data.XGyro);
        WriteIfAbsent(atMs, keys.Gy, data.YGyro);
        WriteIfAbsent(atMs, keys.Gz, data.ZGyro);
        WriteIfAbsent(atMs, keys.Mx, data.XMag);
        WriteIfAbsent(atMs, keys.My, data.YMag);
        WriteIfAbsent(atMs, keys.Mz, data.ZMag);
        WriteIfAbsent(
            atMs,
            keys.MagField,
            Math.Sqrt(
                (double)data.XMag * data.XMag
                + (double)data.YMag * data.YMag
                + (double)data.ZMag * data.ZMag));
        if (data.TemperatureC is { } temp)
        {
            WriteIfAbsent(atMs, keys.Temp, temp);
        }
    }

    private static void WriteHighresImu(Dictionary<string, object> atMs, HighresImuData data)
    {
        var index = data.Id is >= 0 and <= 2 ? data.Id + 1 : 1;
        var keys = ImuKeys.ForIndex(index);
        var ax = data.XAccMs2 / GravityMs2;
        var ay = data.YAccMs2 / GravityMs2;
        var az = data.ZAccMs2 / GravityMs2;
        var mx = data.XMagGauss * 1000.0;
        var my = data.YMagGauss * 1000.0;
        var mz = data.ZMagGauss * 1000.0;

        // Prefer SCALED_IMU when already present for this IMU.
        WriteIfAbsent(atMs, keys.Ax, ax);
        WriteIfAbsent(atMs, keys.Ay, ay);
        WriteIfAbsent(atMs, keys.Az, az);
        WriteIfAbsent(atMs, keys.Gx, data.XGyroRadS);
        WriteIfAbsent(atMs, keys.Gy, data.YGyroRadS);
        WriteIfAbsent(atMs, keys.Gz, data.ZGyroRadS);
        WriteIfAbsent(atMs, keys.Mx, mx);
        WriteIfAbsent(atMs, keys.My, my);
        WriteIfAbsent(atMs, keys.Mz, mz);
        WriteIfAbsent(atMs, keys.AccelSq, MagnitudeSq(ax, ay, az));
        WriteIfAbsent(atMs, keys.GyroSq, MagnitudeSq(data.XGyroRadS, data.YGyroRadS, data.ZGyroRadS));
        WriteIfAbsent(atMs, keys.MagField, Math.Sqrt(MagnitudeSq(mx, my, mz)));
        WriteIfAbsent(atMs, keys.Temp, data.TemperatureC);
        WriteIfAbsent(atMs, FlightFieldIds.PressAbs, data.AbsPressureHpa);
    }

    private static void WriteVibration(Dictionary<string, object> atMs, VibrationData data)
    {
        atMs[FlightFieldIds.VibeX] = data.VibrationX;
        atMs[FlightFieldIds.VibeY] = data.VibrationY;
        atMs[FlightFieldIds.VibeZ] = data.VibrationZ;
        atMs[FlightFieldIds.VibeClip0] = data.Clipping0;
        atMs[FlightFieldIds.VibeClip1] = data.Clipping1;
        atMs[FlightFieldIds.VibeClip2] = data.Clipping2;
    }

    private static void WriteEkf(Dictionary<string, object> atMs, EkfStatusReportData data)
    {
        atMs[FlightFieldIds.EkfVelV] = data.VelocityVariance;
        atMs[FlightFieldIds.EkfPosHor] = data.PosHorizVariance;
        atMs[FlightFieldIds.EkfPosVert] = data.PosVertVariance;
        atMs[FlightFieldIds.EkfCompV] = data.CompassVariance;
        atMs[FlightFieldIds.EkfTerAlt] = data.TerrainAltVariance;
        atMs[FlightFieldIds.EkfFlags] = data.Flags;
        atMs[FlightFieldIds.EkfStatus] = data.Flags;
    }

    private static void WriteCompassmot(Dictionary<string, object> atMs, CompassmotStatusData data)
    {
        atMs[FlightFieldIds.CompassmotCurrentA] = data.CurrentA;
        atMs[FlightFieldIds.CompassmotCompensationX] = data.CompensationX;
        atMs[FlightFieldIds.CompassmotCompensationY] = data.CompensationY;
        atMs[FlightFieldIds.CompassmotCompensationZ] = data.CompensationZ;
        atMs[FlightFieldIds.CompassmotThrottleDpct] = data.ThrottleDpct;
        atMs[FlightFieldIds.CompassmotInterferencePct] = data.InterferencePct;
    }

    private static void WriteIfAbsent(Dictionary<string, object> atMs, string key, object value)
    {
        if (!atMs.ContainsKey(key))
        {
            atMs[key] = value;
        }
    }

    private static double MagnitudeSq(double x, double y, double z) => x * x + y * y + z * z;

    private readonly record struct ImuKeySet(
        string Ax, string Ay, string Az,
        string Gx, string Gy, string Gz,
        string Mx, string My, string Mz,
        string AccelSq, string GyroSq, string MagField, string Temp);

    private static class ImuKeys
    {
        private static readonly ImuKeySet Imu1 = new(
            FlightFieldIds.Ax, FlightFieldIds.Ay, FlightFieldIds.Az,
            FlightFieldIds.Gx, FlightFieldIds.Gy, FlightFieldIds.Gz,
            FlightFieldIds.Mx, FlightFieldIds.My, FlightFieldIds.Mz,
            FlightFieldIds.AccelSq, FlightFieldIds.GyroSq, FlightFieldIds.MagField,
            FlightFieldIds.Imu1Temp);

        private static readonly ImuKeySet Imu2 = new(
            FlightFieldIds.Ax2, FlightFieldIds.Ay2, FlightFieldIds.Az2,
            FlightFieldIds.Gx2, FlightFieldIds.Gy2, FlightFieldIds.Gz2,
            FlightFieldIds.Mx2, FlightFieldIds.My2, FlightFieldIds.Mz2,
            FlightFieldIds.AccelSq2, FlightFieldIds.GyroSq2, FlightFieldIds.MagField2,
            FlightFieldIds.Imu2Temp);

        private static readonly ImuKeySet Imu3 = new(
            FlightFieldIds.Ax3, FlightFieldIds.Ay3, FlightFieldIds.Az3,
            FlightFieldIds.Gx3, FlightFieldIds.Gy3, FlightFieldIds.Gz3,
            FlightFieldIds.Mx3, FlightFieldIds.My3, FlightFieldIds.Mz3,
            FlightFieldIds.AccelSq3, FlightFieldIds.GyroSq3, FlightFieldIds.MagField3,
            FlightFieldIds.Imu3Temp);

        public static ImuKeySet ForIndex(int index) => index switch
        {
            2 => Imu2,
            3 => Imu3,
            _ => Imu1
        };
    }
}
