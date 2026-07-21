using tLogViewer.Core.Enums;
using tLogViewer.Core.Enums.Heartbeat;
using tLogViewer.Core.Models;
using tLogViewer.Core.Models.Messages;
using tLogViewer.DTO.Messages;

namespace tLogViewer.Services.Services;

public static class VehicleHeartbeatSelector
{
    public readonly record struct HeartbeatSample(
        DateTimeOffset Time,
        bool Armed,
        SystemStatus SystemStatus);

    /// <summary>
    /// Returns heartbeat samples from the primary vehicle stream in a raw TLog.
    /// </summary>
    public static IReadOnlyList<HeartbeatSample> SelectVehicleHeartbeats(IEnumerable<TLogRecord> records)
    {
        var candidates = new List<(byte SysId, byte CompId, Heartbeat Heartbeat, ulong Trail)>();

        foreach (var record in records)
        {
            if (record.MavPacket.MsgId != MavMessageTypeId.HEARTBEAT)
            {
                continue;
            }

            var heartbeat = new Heartbeat(record.MavPacket);
            if (heartbeat.Type == AircraftType.Unknown)
            {
                continue;
            }

            candidates.Add((record.MavPacket.SysId, record.MavPacket.CompId, heartbeat, record.Trail));
        }

        return SelectPrimaryStream(
            candidates.Select(static c => (
                c.SysId,
                c.CompId,
                c.Heartbeat.Autopilot,
                c.Heartbeat.Type,
                Sample: new HeartbeatSample(
                    ArmedIntervalFinder.TrailToUtc(c.Trail),
                    c.Heartbeat.BaseMode.HasFlag(MavModeFlag.SafetyArmed),
                    c.Heartbeat.SystemStatus))));
    }

    /// <summary>
    /// Returns heartbeat samples from parsed messages (no SysId/CompId — prefers ArduPilot).
    /// </summary>
    public static IReadOnlyList<HeartbeatSample> SelectFromMessages(IEnumerable<MavMessageDto> messages)
    {
        var candidates = new List<(string Autopilot, string AircraftType, HeartbeatSample Sample)>();

        foreach (var message in messages)
        {
            if (message.Type != "heartbeat" || message.Data is not HeartbeatData heartbeat)
            {
                continue;
            }

            if (string.Equals(heartbeat.AircraftType, AircraftType.Unknown.ToString(), StringComparison.Ordinal))
            {
                continue;
            }

            if (!PowerUpIntervalFinder.TryParseSystemStatus(heartbeat.SystemStatus, out var systemStatus))
            {
                continue;
            }

            candidates.Add((
                heartbeat.Autopilot,
                heartbeat.AircraftType,
                new HeartbeatSample(
                    TlogTime.ParseUtc(message.TimeUtc),
                    heartbeat.Armed,
                    systemStatus)));
        }

        return SelectPrimaryStream(
            candidates.Select(static c => (
                SysId: (byte)0,
                CompId: (byte)0,
                Autopilot: ParseAutopilot(c.Autopilot),
                Type: ParseAircraftType(c.AircraftType),
                Sample: c.Sample)),
            groupBySysComp: false);
    }

    private static IReadOnlyList<HeartbeatSample> SelectPrimaryStream(
        IEnumerable<(byte SysId, byte CompId, Autopilot Autopilot, AircraftType Type, HeartbeatSample Sample)> candidates,
        bool groupBySysComp = true)
    {
        var list = candidates.ToList();
        if (list.Count == 0)
        {
            return Array.Empty<HeartbeatSample>();
        }

        var pool = list.Where(static c => c.Autopilot == Autopilot.ArduPilot).ToList();
        if (pool.Count == 0)
        {
            pool = list.Where(static c => c.Type != AircraftType.Generic).ToList();
        }

        if (pool.Count == 0)
        {
            pool = list;
        }

        IEnumerable<HeartbeatSample> selected;
        if (groupBySysComp)
        {
            var primary = pool
                .GroupBy(static c => (c.SysId, c.CompId))
                .MaxBy(static g => g.Count())!
                .Key;

            selected = pool
                .Where(c => (c.SysId, c.CompId) == primary)
                .Select(static c => c.Sample);
        }
        else
        {
            selected = pool.Select(static c => c.Sample);
        }

        return selected
            .OrderBy(static sample => sample.Time)
            .ToList();
    }

    private static Autopilot ParseAutopilot(string value) =>
        Enum.TryParse<Autopilot>(value, ignoreCase: true, out var autopilot)
            ? autopilot
            : Autopilot.Generic;

    private static AircraftType ParseAircraftType(string value) =>
        Enum.TryParse<AircraftType>(value, ignoreCase: true, out var type)
            ? type
            : AircraftType.Unknown;
}
