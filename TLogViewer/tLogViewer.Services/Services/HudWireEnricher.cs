using tLogViewer.Core.Models;
using tLogViewer.DTO.Messages;

namespace tLogViewer.Services.Services;

/// <summary>Maps newly-supported wire messages onto MAVLink messageId_* HUD keys.</summary>
public static class HudWireEnricher
{
    private static readonly string?[] ChOutPort0 =
    [
        FlightFieldIds.Ch1out, FlightFieldIds.Ch2out, FlightFieldIds.Ch3out, FlightFieldIds.Ch4out,
        FlightFieldIds.Ch5out, FlightFieldIds.Ch6out, FlightFieldIds.Ch7out, FlightFieldIds.Ch8out,
        FlightFieldIds.Ch9out, FlightFieldIds.Ch10out, FlightFieldIds.Ch11out, FlightFieldIds.Ch12out,
        FlightFieldIds.Ch13out, FlightFieldIds.Ch14out, FlightFieldIds.Ch15out, FlightFieldIds.Ch16out,
    ];

    private static readonly string?[] ChOutPort1 =
    [
        FlightFieldIds.Ch17out, FlightFieldIds.Ch18out, null, FlightFieldIds.Ch20out,
        FlightFieldIds.Ch21out, FlightFieldIds.Ch22out, FlightFieldIds.Ch23out, FlightFieldIds.Ch24out,
        FlightFieldIds.Ch25out, FlightFieldIds.Ch26out, FlightFieldIds.Ch27out, FlightFieldIds.Ch28out,
        FlightFieldIds.Ch29out, FlightFieldIds.Ch30out, FlightFieldIds.Ch31out, FlightFieldIds.Ch32out,
    ];

    private static readonly string[] RangefinderKeys =
    [
        FlightFieldIds.Rangefinder1, FlightFieldIds.Rangefinder2, FlightFieldIds.Rangefinder3,
        FlightFieldIds.Rangefinder4, FlightFieldIds.Rangefinder5, FlightFieldIds.Rangefinder6,
        FlightFieldIds.Rangefinder7, FlightFieldIds.Rangefinder8, FlightFieldIds.Rangefinder9,
        FlightFieldIds.Rangefinder10,
    ];

    private static readonly (string Curr, string Rpm, string Temp, string Volt)[] EscKeys =
    [
        (FlightFieldIds.Esc1Curr, FlightFieldIds.Esc1Rpm, FlightFieldIds.Esc1Temp, FlightFieldIds.Esc1Volt),
        (FlightFieldIds.Esc2Curr, FlightFieldIds.Esc2Rpm, FlightFieldIds.Esc2Temp, FlightFieldIds.Esc2Volt),
        (FlightFieldIds.Esc3Curr, FlightFieldIds.Esc3Rpm, FlightFieldIds.Esc3Temp, FlightFieldIds.Esc3Volt),
        (FlightFieldIds.Esc4Curr, FlightFieldIds.Esc4Rpm, FlightFieldIds.Esc4Temp, FlightFieldIds.Esc4Volt),
        (FlightFieldIds.Esc5Curr, FlightFieldIds.Esc5Rpm, FlightFieldIds.Esc5Temp, FlightFieldIds.Esc5Volt),
        (FlightFieldIds.Esc6Curr, FlightFieldIds.Esc6Rpm, FlightFieldIds.Esc6Temp, FlightFieldIds.Esc6Volt),
        (FlightFieldIds.Esc7Curr, FlightFieldIds.Esc7Rpm, FlightFieldIds.Esc7Temp, FlightFieldIds.Esc7Volt),
        (FlightFieldIds.Esc8Curr, FlightFieldIds.Esc8Rpm, FlightFieldIds.Esc8Temp, FlightFieldIds.Esc8Volt),
        (FlightFieldIds.Esc9Curr, FlightFieldIds.Esc9Rpm, FlightFieldIds.Esc9Temp, FlightFieldIds.Esc9Volt),
        (FlightFieldIds.Esc10Curr, FlightFieldIds.Esc10Rpm, FlightFieldIds.Esc10Temp, FlightFieldIds.Esc10Volt),
        (FlightFieldIds.Esc11Curr, FlightFieldIds.Esc11Rpm, FlightFieldIds.Esc11Temp, FlightFieldIds.Esc11Volt),
        (FlightFieldIds.Esc12Curr, FlightFieldIds.Esc12Rpm, FlightFieldIds.Esc12Temp, FlightFieldIds.Esc12Volt),
        (FlightFieldIds.Esc13Curr, FlightFieldIds.Esc13Rpm, FlightFieldIds.Esc13Temp, FlightFieldIds.Esc13Volt),
        (FlightFieldIds.Esc14Curr, FlightFieldIds.Esc14Rpm, FlightFieldIds.Esc14Temp, FlightFieldIds.Esc14Volt),
        (FlightFieldIds.Esc15Curr, FlightFieldIds.Esc15Rpm, FlightFieldIds.Esc15Temp, FlightFieldIds.Esc15Volt),
        (FlightFieldIds.Esc16Curr, FlightFieldIds.Esc16Rpm, FlightFieldIds.Esc16Temp, FlightFieldIds.Esc16Volt),
    ];

    public static bool TryPush(Dictionary<string, object> atMs, MavMessageDto message)
    {
        switch (message.Data)
        {
            case ScaledPressureData p:
                WritePressure(atMs, p);
                return true;
            case ServoOutputRawData s:
                WriteServo(atMs, s);
                return true;
            case OpticalFlowData o:
                atMs[FlightFieldIds.OptMX] = o.FlowCompMX;
                atMs[FlightFieldIds.OptMY] = o.FlowCompMY;
                atMs[FlightFieldIds.OptX] = o.FlowX;
                atMs[FlightFieldIds.OptY] = o.FlowY;
                atMs[FlightFieldIds.OptQua] = o.Quality;
                return true;
            case Gps2RawData g:
                WriteGps2(atMs, g);
                return true;
            case PowerStatusData pw:
                atMs[FlightFieldIds.BoardVoltage] = pw.BoardVoltageV;
                atMs[FlightFieldIds.ServoVoltage] = pw.ServoVoltageV;
                atMs[FlightFieldIds.VoltageFlag] = pw.VoltageFlag;
                return true;
            case DistanceSensorData d:
                if (d.Id < RangefinderKeys.Length)
                {
                    atMs[RangefinderKeys[d.Id]] = d.CurrentDistanceCm;
                }

                return true;
            case TerrainReportData t:
                atMs[FlightFieldIds.TerAlt] = t.TerrainHeightM;
                atMs[FlightFieldIds.TerCurAlt] = t.CurrentHeightM;
                atMs[FlightFieldIds.TerPend] = t.Pending;
                atMs[FlightFieldIds.TerLoad] = t.Loaded;
                atMs[FlightFieldIds.TerSpace] = t.Spacing;
                return true;
            case MemInfoData m:
                atMs[FlightFieldIds.BrkLevel] = m.BrkLevel;
                atMs[FlightFieldIds.Freemem] = m.Freemem;
                return true;
            case MountStatusData mt:
                atMs[FlightFieldIds.CampointA] = mt.PointingADeg;
                atMs[FlightFieldIds.CampointB] = mt.PointingBDeg;
                atMs[FlightFieldIds.CampointC] = mt.PointingCDeg;
                return true;
            case FenceStatusData f:
                atMs[FlightFieldIds.FencebStatus] = f.BreachStatus;
                atMs[FlightFieldIds.FencebCount] = f.BreachCount;
                atMs[FlightFieldIds.FencebType] = f.BreachType;
                return true;
            case HwStatusData h:
                atMs[FlightFieldIds.HwVoltage] = h.HwVoltageV;
                atMs[FlightFieldIds.I2cErrors] = h.I2cErrors;
                return true;
            case RangefinderData r:
                atMs[FlightFieldIds.SonarRange] = r.DistanceM;
                atMs[FlightFieldIds.SonarVoltage] = r.VoltageV;
                return true;
            case AirspeedAutocalData a:
                atMs[FlightFieldIds.AsRatio] = a.Ratio;
                return true;
            case Ahrs2Data ahrs:
                atMs[FlightFieldIds.Ahrs2Roll] = ahrs.RollDeg;
                atMs[FlightFieldIds.Ahrs2Pitch] = ahrs.PitchDeg;
                atMs[FlightFieldIds.Ahrs2Yaw] = ahrs.YawDeg;
                atMs[FlightFieldIds.Ahrs2Alt] = ahrs.AltitudeM;
                atMs[FlightFieldIds.Ahrs2Lat] = ahrs.LatitudeDeg;
                atMs[FlightFieldIds.Ahrs2Lng] = ahrs.LongitudeDeg;
                return true;
            case PidTuningData pid:
                WritePid(atMs, pid);
                return true;
            case RpmData rpm:
                atMs[FlightFieldIds.Rpm1] = rpm.Rpm1;
                atMs[FlightFieldIds.Rpm2] = rpm.Rpm2;
                return true;
            case EfiStatusData efi:
                WriteEfi(atMs, efi);
                return true;
            case ExtendedSysStateData ext:
                atMs[FlightFieldIds.VtolState] = ext.VtolState;
                atMs[FlightFieldIds.LandedState] = ext.LandedState;
                return true;
            case AutopilotVersionData ver:
                atMs[FlightFieldIds.Capabilities] = ver.Capabilities;
                atMs[FlightFieldIds.Uid] = ver.Uid;
                return true;
            case GeneratorStatusData gen:
                atMs[FlightFieldIds.GenStatus] = gen.Status;
                atMs[FlightFieldIds.GenSpeed] = gen.GeneratorSpeed;
                atMs[FlightFieldIds.GenCurrent] = gen.BatteryCurrentA;
                atMs[FlightFieldIds.GenVoltage] = gen.BusVoltageV;
                atMs[FlightFieldIds.GenRuntime] = gen.RuntimeSec;
                atMs[FlightFieldIds.GenMaintTime] = gen.TimeUntilMaintenanceSec;
                return true;
            case EscTelemetryData esc:
                WriteEscTelemetry(atMs, esc);
                return true;
            case EscInfoData escInfo:
                WriteEscInfo(atMs, escInfo);
                return true;
            case EscStatusData escStatus:
                WriteEscStatus(atMs, escStatus);
                return true;
            case McuStatusData mcu:
                atMs[FlightFieldIds.McuTemp] = mcu.TemperatureC;
                atMs[FlightFieldIds.McuVoltage] = mcu.VoltageV;
                atMs[FlightFieldIds.McuMinVolt] = mcu.VoltageMinV;
                atMs[FlightFieldIds.McuMaxVolt] = mcu.VoltageMaxV;
                return true;
            case HygrometerSensorData hygro:
                if (hygro.Id == 0)
                {
                    atMs[FlightFieldIds.HygroTemp1] = hygro.TemperatureC;
                    atMs[FlightFieldIds.HygroHumi1] = hygro.HumidityPct;
                }
                else
                {
                    atMs[FlightFieldIds.HygroTemp2] = hygro.TemperatureC;
                    atMs[FlightFieldIds.HygroHumi2] = hygro.HumidityPct;
                }

                return true;
            case AoaSsaData aoa:
                atMs[FlightFieldIds.Aoa] = aoa.AoaDeg;
                atMs[FlightFieldIds.Ssa] = aoa.SsaDeg;
                return true;
            case UavionixAdsbOutStatusData xpdr:
                WriteXpdr(atMs, xpdr);
                return true;
            default:
                return false;
        }
    }

    private static void WritePressure(Dictionary<string, object> atMs, ScaledPressureData p)
    {
        if (p.SensorIndex <= 1)
        {
            atMs[FlightFieldIds.PressAbs] = p.PressAbsHpa;
            atMs[FlightFieldIds.PressTemp] = p.TemperatureC;
            if (p.TemperaturePressDiffC is { } t)
            {
                atMs[FlightFieldIds.Airspeed1Temp] = t;
            }
        }
        else
        {
            atMs[FlightFieldIds.PressAbs2] = p.PressAbsHpa;
            atMs[FlightFieldIds.PressTemp2] = p.TemperatureC;
            if (p.TemperaturePressDiffC is { } t)
            {
                atMs[FlightFieldIds.Airspeed2Temp] = t;
            }
        }
    }

    private static void WriteServo(Dictionary<string, object> atMs, ServoOutputRawData s)
    {
        var keys = s.Port == 0 ? ChOutPort0 : ChOutPort1;
        var count = Math.Min(s.ServoRaw.Length, keys.Length);
        for (var i = 0; i < count; i++)
        {
            if (keys[i] is { } key)
            {
                atMs[key] = s.ServoRaw[i];
            }
        }
    }

    private static void WriteGps2(Dictionary<string, object> atMs, Gps2RawData g)
    {
        atMs[FlightFieldIds.Lat2] = g.LatitudeDeg;
        atMs[FlightFieldIds.Lng2] = g.LongitudeDeg;
        atMs[FlightFieldIds.AltAsl2] = g.AltitudeM;
        atMs[FlightFieldIds.GpsStatus2] = g.FixType;
        atMs[FlightFieldIds.Satcount2] = g.SatellitesVisible;
        atMs[FlightFieldIds.Groundspeed2] = g.GroundSpeedMS;
        atMs[FlightFieldIds.Groundcourse2] = g.CourseOverGroundDeg;
        if (g.Eph != ushort.MaxValue)
        {
            atMs[FlightFieldIds.GpsHdop2] = g.Eph / 100.0;
        }

        if (g.HAcc is { } h and not uint.MaxValue)
        {
            atMs[FlightFieldIds.GpsHAcc2] = h / 1000.0;
        }

        if (g.VAcc is { } v and not uint.MaxValue)
        {
            atMs[FlightFieldIds.GpsVAcc2] = v / 1000.0;
        }

        if (g.VelAcc is { } vel and not uint.MaxValue)
        {
            atMs[FlightFieldIds.GpsVelAcc2] = vel / 1000.0;
        }

        if (g.HdgAcc is { } hdg and not uint.MaxValue)
        {
            atMs[FlightFieldIds.GpsHdgAcc2] = hdg / 1e5;
        }

        if (g.YawCdeg is { } yaw and not ushort.MaxValue)
        {
            atMs[FlightFieldIds.GpsYaw2] = yaw / 100.0;
        }
    }

    private static void WritePid(Dictionary<string, object> atMs, PidTuningData pid)
    {
        atMs[FlightFieldIds.PidAxis] = pid.Axis;
        atMs[FlightFieldIds.PidDesired] = pid.Desired;
        atMs[FlightFieldIds.PidAchieved] = pid.Achieved;
        atMs[FlightFieldIds.PidFf] = pid.Ff;
        atMs[FlightFieldIds.PidP] = pid.P;
        atMs[FlightFieldIds.PidL] = pid.I;
        atMs[FlightFieldIds.PidD] = pid.D;
        if (pid.SRate is { } srate)
        {
            atMs[FlightFieldIds.PidSRate] = srate;
            switch (pid.Axis)
            {
                case 1:
                    atMs[FlightFieldIds.PidSRateRoll] = srate;
                    break;
                case 2:
                    atMs[FlightFieldIds.PidSRatePitch] = srate;
                    break;
                case 3:
                    atMs[FlightFieldIds.PidSRateYaw] = srate;
                    break;
                case 4:
                    atMs[FlightFieldIds.PidSRateAccZ] = srate;
                    break;
                case 5:
                    atMs[FlightFieldIds.PidSRateSteer] = srate;
                    break;
                case 6:
                    atMs[FlightFieldIds.PidSRateLanding] = srate;
                    break;
            }
        }
    }

    private static void WriteEfi(Dictionary<string, object> atMs, EfiStatusData efi)
    {
        atMs[FlightFieldIds.EfiHealth] = efi.Health;
        atMs[FlightFieldIds.EfiRpm] = efi.Rpm;
        atMs[FlightFieldIds.EfiFuelConsumed] = efi.FuelConsumedCm3;
        atMs[FlightFieldIds.EfiFuelFlow] = efi.FuelFlowCm3Min;
        atMs[FlightFieldIds.EfiLoad] = efi.EngineLoadPct;
        atMs[FlightFieldIds.EfiBaro] = efi.BarometricPressureKpa;
        atMs[FlightFieldIds.EfiIntakeTemp] = efi.IntakeManifoldTemperatureC;
        atMs[FlightFieldIds.EfiHeadTemp] = efi.CylinderHeadTemperatureC;
        atMs[FlightFieldIds.EfiExhaustTemp] = efi.ExhaustGasTemperatureC;
        atMs[FlightFieldIds.EfiFuelPressure] = efi.FuelPressureKpa;
    }

    /// <summary>
    /// ESC curr/rpm/temp/volt are unified onto DERIVED (998) keys because a log may carry
    /// ESC_TELEMETRY_*, ESC_STATUS, and/or ESC_INFO. Slots with all-zero values are skipped.
    /// </summary>
    private static void WriteEscTelemetry(Dictionary<string, object> atMs, EscTelemetryData esc)
    {
        var start = Math.Max(1, esc.FirstEscIndex) - 1;
        for (var i = 0; i < 4; i++)
        {
            WriteEscSlotIfNonZero(
                atMs,
                start + i,
                esc.CurrentCa[i] / 100.0,
                esc.RpmErpm[i],
                esc.TemperatureDegC[i],
                esc.VoltageCv[i] / 100.0);
        }
    }

    private static void WriteEscInfo(Dictionary<string, object> atMs, EscInfoData esc)
    {
        var start = esc.Index;
        var total = esc.Count == 0 ? int.MaxValue : esc.Count;
        for (var i = 0; i < 4; i++)
        {
            var abs = start + i;
            if (abs >= total)
            {
                continue;
            }

            var raw = esc.TemperatureCdegC[i];
            if (raw == short.MaxValue)
            {
                continue;
            }

            WriteEscSlotIfNonZero(atMs, abs, curr: null, rpm: null, temp: raw / 100.0, volt: null);
        }
    }

    private static void WriteEscStatus(Dictionary<string, object> atMs, EscStatusData esc)
    {
        var start = esc.Index;
        for (var i = 0; i < 4; i++)
        {
            var curr = FiniteOrNull(esc.CurrentA[i]);
            var volt = FiniteOrNull(esc.VoltageV[i]);
            var rpm = (double)esc.Rpm[i];
            WriteEscSlotIfNonZero(atMs, start + i, curr, rpm, temp: null, volt);
        }
    }

    private static void WriteEscSlotIfNonZero(
        Dictionary<string, object> atMs,
        int zeroBasedEscIndex,
        double? curr,
        double? rpm,
        double? temp,
        double? volt)
    {
        if (zeroBasedEscIndex < 0 || zeroBasedEscIndex >= EscKeys.Length)
        {
            return;
        }

        var hasNonZero =
            (curr is { } c && c != 0)
            || (rpm is { } r && r != 0)
            || (temp is { } t && t != 0)
            || (volt is { } v && v != 0);
        if (!hasNonZero)
        {
            return;
        }

        var keys = EscKeys[zeroBasedEscIndex];
        if (curr is { } currVal)
        {
            atMs[keys.Curr] = currVal;
        }

        if (rpm is { } rpmVal)
        {
            atMs[keys.Rpm] = Math.Abs(rpmVal);
        }

        if (temp is { } tempVal)
        {
            atMs[keys.Temp] = tempVal;
        }

        if (volt is { } voltVal)
        {
            atMs[keys.Volt] = voltVal;
        }
    }

    private static double? FiniteOrNull(float value) =>
        float.IsFinite(value) ? value : null;

    private static void WriteXpdr(Dictionary<string, object> atMs, UavionixAdsbOutStatusData xpdr)
    {
        atMs["10008_011"] = xpdr.Squawk;
        atMs["10008_003"] = xpdr.BoardTempC;
        atMs["10008_014"] = (xpdr.NicNacp >> 4) & 0xF;
        atMs["10008_015"] = xpdr.NicNacp & 0xF;
        atMs["10008_002"] = (xpdr.State & 0x01) != 0 ? 1 : 0;
        atMs["10008_007"] = (xpdr.State & 0x02) != 0 ? 1 : 0;
        atMs["10008_010"] = (xpdr.State & 0x04) != 0 ? 1 : 0;
        atMs["10008_012"] = (xpdr.State & 0x08) != 0 ? 1 : 0;
        atMs["10008_013"] = (xpdr.State & 0x10) != 0 ? 1 : 0;
        atMs["10008_004"] = (xpdr.State & 0x20) != 0 ? 1 : 0;
        atMs["10008_001"] = (xpdr.Fault & 0x01) != 0 ? 1 : 0;
        atMs["10008_005"] = (xpdr.Fault & 0x02) != 0 ? 1 : 0;
        atMs["10008_006"] = (xpdr.Fault & 0x04) != 0 ? 1 : 0;
        atMs["10008_009"] = (xpdr.Fault & 0x08) != 0 ? 1 : 0;
        atMs["10008_017"] = (xpdr.Fault & 0x10) != 0 ? 1 : 0;
        atMs["10008_016"] = (xpdr.Fault & 0x20) != 0 ? 1 : 0;
        atMs["10008_008"] = (xpdr.Fault & 0x40) != 0 ? 1 : 0;
        atMs["10008_018"] = (xpdr.Fault & 0x80) != 0 ? 1 : 0;
    }
}
