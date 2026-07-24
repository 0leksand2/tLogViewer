using tLogViewer.Core.Models;
using tLogViewer.DTO.Messages;

namespace tLogViewer.Services.Services;

/// <summary>
/// Maps already-parsed wire messages onto Mission Planner 999_* HUD keys
/// without replacing the normal flattened {messageId}_* fields.
/// </summary>
public static class HudAliasEnricher
{
    private static readonly string[] ChInKeys =
    [
        FlightFieldIds.Ch1in, FlightFieldIds.Ch2in, FlightFieldIds.Ch3in, FlightFieldIds.Ch4in,
        FlightFieldIds.Ch5in, FlightFieldIds.Ch6in, FlightFieldIds.Ch7in, FlightFieldIds.Ch8in,
        FlightFieldIds.Ch9in, FlightFieldIds.Ch10in, FlightFieldIds.Ch11in, FlightFieldIds.Ch12in,
        FlightFieldIds.Ch13in, FlightFieldIds.Ch14in, FlightFieldIds.Ch15in, FlightFieldIds.Ch16in,
    ];

    /// <summary>Writes HUD aliases for known message DTOs. Safe to call after flattening.</summary>
    public static void Apply(Dictionary<string, object> atMs, MavMessageDto message)
    {
        switch (message.Data)
        {
            case GpsRawIntData gps:
                WriteGpsRaw(atMs, gps);
                break;
            case LocalPositionNedData ned:
                atMs[FlightFieldIds.Posn] = ned.X;
                atMs[FlightFieldIds.Pose] = ned.Y;
                atMs[FlightFieldIds.Posd] = ned.Z;
                break;
            case GlobalPositionIntData gpi:
                atMs[FlightFieldIds.Vx] = gpi.VelocityXMs;
                atMs[FlightFieldIds.Vy] = gpi.VelocityYMs;
                atMs[FlightFieldIds.Vz] = gpi.VelocityZMs;
                break;
            case RcChannelsData rc:
                WriteRcIn(atMs, rc);
                break;
            case RadioData radio:
                atMs[FlightFieldIds.Fixedp] = radio.Fixed;
                break;
            case SysStatusData sys:
                WriteSysStatus(atMs, sys);
                break;
        }
    }

    private static void WriteGpsRaw(Dictionary<string, object> atMs, GpsRawIntData gps)
    {
        if (gps.Eph != ushort.MaxValue)
        {
            atMs[FlightFieldIds.GpsHdop] = gps.Eph / 100.0;
        }

        atMs[FlightFieldIds.Groundcourse] = gps.CourseOverGroundDeg;

        if (gps.HAcc is { } hAcc and not uint.MaxValue)
        {
            atMs[FlightFieldIds.GpsHAcc] = hAcc / 1000.0;
        }

        if (gps.VAcc is { } vAcc and not uint.MaxValue)
        {
            atMs[FlightFieldIds.GpsVAcc] = vAcc / 1000.0;
        }

        if (gps.VelAcc is { } velAcc and not uint.MaxValue)
        {
            atMs[FlightFieldIds.GpsVelAcc] = velAcc / 1000.0;
        }

        if (gps.HdgAcc is { } hdgAcc and not uint.MaxValue)
        {
            atMs[FlightFieldIds.GpsHdgAcc] = hdgAcc / 1e5;
        }

        if (gps.YawCdeg is { } yaw and not ushort.MaxValue)
        {
            atMs[FlightFieldIds.GpsYaw] = yaw / 100.0;
        }
    }

    private static void WriteRcIn(Dictionary<string, object> atMs, RcChannelsData rc)
    {
        var count = Math.Min(rc.Channels.Count, ChInKeys.Length);
        for (var i = 0; i < count; i++)
        {
            // Stored as normalized stick; convert back to PWM-style for HUD.
            atMs[ChInKeys[i]] = 1500.0 + rc.Channels[i] * 500.0;
        }

        atMs[FlightFieldIds.RxRssi] = rc.Rssi;
    }

    private static void WriteSysStatus(Dictionary<string, object> atMs, SysStatusData sys)
    {
        atMs[FlightFieldIds.PacketDropRemote] = sys.DropRateComm;
        atMs[FlightFieldIds.ErrorsCount1] = sys.Errors1;
        atMs[FlightFieldIds.ErrorsCount2] = sys.Errors2;
        atMs[FlightFieldIds.ErrorsCount3] = sys.Errors3;
        atMs[FlightFieldIds.ErrorsCount4] = sys.Errors4;
        atMs[FlightFieldIds.TerrainActive] = sys.TerrainActive ? 1 : 0;
        atMs[FlightFieldIds.SafetyActive] = sys.SafetyActive ? 1 : 0;
        atMs[FlightFieldIds.PrearmStatus] = sys.PrearmStatus ? 1 : 0;
    }
}
