using tLogViewer.Core.Models;
using tLogViewer.DTO.Messages;

namespace tLogViewer.Services.Services;

/// <summary>
/// Maps BATTERY_STATUS onto Mission Planner battery_cell* / battery_voltage* keys,
/// or estimates equal cell voltages from SYS_STATUS pack voltage when per-cell data is absent.
/// </summary>
public static class BatteryFieldsEnricher
{
    /// <summary>Full LiPo cell charge used for fallback cell-count math.</summary>
    private const double MaxCellVoltageV = 4.2;

    /// <summary>Individual cell readings fall in this band (volts).</summary>
    private const double MinIndividualCellV = 2.0;
    private const double MaxIndividualCellV = 4.5;

    private const ushort UnusedCellMv = ushort.MaxValue;
    private const int MaxEstimatedCells = 12;

    /// <summary>
    /// Known pack windows from empty → full (inclusive).
    /// 6S uses 22.2–25.2 V (nominal → max); others use the same 3.7n–4.2n pattern,
    /// widened slightly so a partially discharged pack still matches.
    /// </summary>
    private static readonly (int Cells, double MinV, double MaxV)[] PackVoltageWindows =
    [
        // 1S: 3.7–4.2 (allow empty ~3.0)
        (1, 2.8, 4.35),
        // 3S: 11.1–12.6
        (3, 9.0, 13.0),
        // 6S: 22.2–25.2
        (6, 19.0, 26.0),
        // 12S: 44.4–50.4
        (12, 38.0, 52.0),
    ];

    private static readonly string SysBatteryVoltageKey = FlightFieldIds.BatteryVoltageV;

    private static readonly string[] CellKeys =
    [
        FlightFieldIds.BatteryCell1,
        FlightFieldIds.BatteryCell2,
        FlightFieldIds.BatteryCell3,
        FlightFieldIds.BatteryCell4,
        FlightFieldIds.BatteryCell5,
        FlightFieldIds.BatteryCell6,
        FlightFieldIds.BatteryCell7,
        FlightFieldIds.BatteryCell8,
        FlightFieldIds.BatteryCell9,
        FlightFieldIds.BatteryCell10,
        FlightFieldIds.BatteryCell11,
        FlightFieldIds.BatteryCell12,
        FlightFieldIds.BatteryCell13,
        FlightFieldIds.BatteryCell14,
    ];

    private static readonly string[] ExtraPackVoltageKeys =
    [
        FlightFieldIds.BatteryVoltage2,
        FlightFieldIds.BatteryVoltage3,
        FlightFieldIds.BatteryVoltage4,
        FlightFieldIds.BatteryVoltage5,
        FlightFieldIds.BatteryVoltage6,
        FlightFieldIds.BatteryVoltage7,
        FlightFieldIds.BatteryVoltage8,
        FlightFieldIds.BatteryVoltage9,
    ];

    private static readonly string[] ExtraRemainingKeys =
    [
        FlightFieldIds.BatteryRemaining2,
        FlightFieldIds.BatteryRemaining3,
        FlightFieldIds.BatteryRemaining4,
        FlightFieldIds.BatteryRemaining5,
        FlightFieldIds.BatteryRemaining6,
        FlightFieldIds.BatteryRemaining7,
        FlightFieldIds.BatteryRemaining8,
        FlightFieldIds.BatteryRemaining9,
    ];

    private static readonly string[] CurrentKeys =
    [
        "1_001",
        FlightFieldIds.Current2,
        FlightFieldIds.Current3,
        FlightFieldIds.Current4,
        FlightFieldIds.Current5,
        FlightFieldIds.Current6,
        FlightFieldIds.Current7,
        FlightFieldIds.Current8,
        FlightFieldIds.Current9,
    ];

    private static readonly string[] TempKeys =
    [
        FlightFieldIds.BatteryTemp,
        FlightFieldIds.BatteryTemp2,
        FlightFieldIds.BatteryTemp3,
        FlightFieldIds.BatteryTemp4,
        FlightFieldIds.BatteryTemp5,
        FlightFieldIds.BatteryTemp6,
        FlightFieldIds.BatteryTemp7,
        FlightFieldIds.BatteryTemp8,
        FlightFieldIds.BatteryTemp9,
    ];

    private static readonly string[] UsedMahKeys =
    [
        FlightFieldIds.BatteryUsedMah,
        FlightFieldIds.BatteryUsedMah2,
        FlightFieldIds.BatteryUsedMah3,
        FlightFieldIds.BatteryUsedMah4,
        FlightFieldIds.BatteryUsedMah5,
        FlightFieldIds.BatteryUsedMah6,
        FlightFieldIds.BatteryUsedMah7,
        FlightFieldIds.BatteryUsedMah8,
        FlightFieldIds.BatteryUsedMah9,
    ];

    private static readonly string[] RemainMinKeys =
    [
        FlightFieldIds.BatteryRemainMin,
        FlightFieldIds.BatteryRemainMin2,
        FlightFieldIds.BatteryRemainMin3,
        FlightFieldIds.BatteryRemainMin4,
        FlightFieldIds.BatteryRemainMin5,
        FlightFieldIds.BatteryRemainMin6,
        FlightFieldIds.BatteryRemainMin7,
        FlightFieldIds.BatteryRemainMin8,
        FlightFieldIds.BatteryRemainMin9,
    ];

    /// <summary>
    /// Writes BATTERY_STATUS fields into the millisecond bucket (Mission Planner 999_* keys).
    /// Returns true when the message was a BATTERY_STATUS (even if no cells were written).
    /// </summary>
    public static bool TryPush(
        Dictionary<string, object> atMs,
        MavMessageDto message,
        out bool wroteIndividualCells)
    {
        wroteIndividualCells = false;

        if (!string.Equals(message.Type, "batteryStatus", StringComparison.Ordinal)
            && !string.Equals(message.MessageId, "147", StringComparison.Ordinal))
        {
            return false;
        }

        if (message.Data is not BatteryStatusData data)
        {
            return false;
        }

        wroteIndividualCells = ApplyBatteryStatus(atMs, data);
        return true;
    }

    /// <summary>
    /// Fills battery_cell1..N from SYS_STATUS pack voltage on milliseconds that still lack cell data.
    /// Cell count from 1S/3S/6S/12S voltage windows (else ceil(packV / 4.2)); each cell = packV / N.
    /// </summary>
    public static void EstimateMissingCellsFromSysStatus(
        Dictionary<long, Dictionary<string, object>> byMillisecond)
    {
        if (byMillisecond.Count == 0)
        {
            return;
        }

        foreach (var atMs in byMillisecond.Values)
        {
            if (atMs.ContainsKey(CellKeys[0]))
            {
                continue;
            }

            if (!TryAsDouble(atMs, SysBatteryVoltageKey, out var packVoltageV) || packVoltageV <= 0)
            {
                continue;
            }

            WriteEqualCellVoltages(atMs, packVoltageV);
        }
    }

    /// <summary>Forward-fills cell and extra-pack battery keys across the flight timeline.</summary>
    public static void ForwardFill(Dictionary<long, Dictionary<string, object>> byMillisecond)
    {
        if (byMillisecond.Count == 0)
        {
            return;
        }

        var lastValues = new Dictionary<string, object>(StringComparer.Ordinal);

        foreach (var ms in byMillisecond.Keys.OrderBy(static key => key))
        {
            var atMs = byMillisecond[ms];

            foreach (var key in CellKeys)
            {
                ForwardFillKey(atMs, lastValues, key);
            }

            foreach (var key in ExtraPackVoltageKeys)
            {
                ForwardFillKey(atMs, lastValues, key);
            }

            foreach (var key in ExtraRemainingKeys)
            {
                ForwardFillKey(atMs, lastValues, key);
            }

            foreach (var key in CurrentKeys)
            {
                ForwardFillKey(atMs, lastValues, key);
            }

            foreach (var key in TempKeys)
            {
                ForwardFillKey(atMs, lastValues, key);
            }

            foreach (var key in UsedMahKeys)
            {
                ForwardFillKey(atMs, lastValues, key);
            }

            foreach (var key in RemainMinKeys)
            {
                ForwardFillKey(atMs, lastValues, key);
            }
        }
    }

    /// <returns>True when individual per-cell voltages were written.</returns>
    private static bool ApplyBatteryStatus(Dictionary<string, object> atMs, BatteryStatusData data)
    {
        var cellVoltages = ResolveIndividualCellVoltagesV(data.VoltagesMv, data.VoltagesExtMv);
        var packVoltageV = ResolvePackVoltageV(data.VoltagesMv, data.VoltagesExtMv);
        var wroteCells = false;

        WriteBatteryExtras(atMs, data);

        if (data.Id == 0)
        {
            if (cellVoltages is { Length: > 0 })
            {
                for (var i = 0; i < cellVoltages.Length && i < CellKeys.Length; i++)
                {
                    atMs[CellKeys[i]] = cellVoltages[i];
                }

                wroteCells = true;
            }

            return wroteCells;
        }

        if (data.Id >= 1 && data.Id <= 8 && packVoltageV is > 0)
        {
            atMs[ExtraPackVoltageKeys[data.Id - 1]] = packVoltageV.Value;
            if (data.BatteryRemainingPct >= 0)
            {
                atMs[ExtraRemainingKeys[data.Id - 1]] = (double)data.BatteryRemainingPct;
            }
        }

        return false;
    }

    private static void WriteBatteryExtras(Dictionary<string, object> atMs, BatteryStatusData data)
    {
        if (data.Id > 8)
        {
            return;
        }

        if (data.CurrentBatteryA is { } current)
        {
            atMs[CurrentKeys[data.Id]] = current;
        }

        if (data.TemperatureC is { } temp)
        {
            atMs[TempKeys[data.Id]] = temp;
        }

        if (data.CurrentConsumedMah is { } used)
        {
            atMs[UsedMahKeys[data.Id]] = used;
        }

        if (data.TimeRemainingSec is { } remainSec and > 0)
        {
            atMs[RemainMinKeys[data.Id]] = remainSec / 60.0;
        }
    }

    private static void WriteEqualCellVoltages(Dictionary<string, object> atMs, double packVoltageV)
    {
        var cellCount = EstimateCellCount(packVoltageV);
        var cellVoltageV = packVoltageV / cellCount;
        for (var i = 0; i < cellCount; i++)
        {
            atMs[CellKeys[i]] = cellVoltageV;
        }
    }

    /// <summary>
    /// Series cell count from pack voltage: match 1S / 3S / 6S / 12S windows when possible
    /// (6S ≈ 22.2–25.2 V), otherwise ceil(packV / 4.2).
    /// </summary>
    public static int EstimateCellCount(double packVoltageV)
    {
        if (packVoltageV <= 0)
        {
            return 1;
        }

        foreach (var (cells, minV, maxV) in PackVoltageWindows)
        {
            if (packVoltageV >= minV && packVoltageV <= maxV)
            {
                return cells;
            }
        }

        return Math.Clamp(
            (int)Math.Ceiling(packVoltageV / MaxCellVoltageV - 1e-9),
            1,
            MaxEstimatedCells);
    }

    private static double? ResolvePackVoltageV(ushort[] voltagesMv, ushort[] voltagesExtMv)
    {
        double sumMv = 0;
        var any = false;

        foreach (var mv in voltagesMv)
        {
            if (mv == UnusedCellMv || mv == 0)
            {
                continue;
            }

            sumMv += mv;
            any = true;
        }

        foreach (var mv in voltagesExtMv)
        {
            if (mv == 0)
            {
                continue;
            }

            sumMv += mv;
            any = true;
        }

        return any ? sumMv / 1000.0 : null;
    }

    /// <summary>
    /// Per-cell voltages when BATTERY_STATUS reports 2+ readings in the LiPo cell band.
    /// Null for pack-total / split-pack encodings.
    /// </summary>
    private static double[]? ResolveIndividualCellVoltagesV(ushort[] voltagesMv, ushort[] voltagesExtMv)
    {
        var cells = new List<double>(14);

        foreach (var mv in voltagesMv)
        {
            if (mv == UnusedCellMv || mv == 0)
            {
                continue;
            }

            var volts = mv / 1000.0;
            if (volts < MinIndividualCellV || volts > MaxIndividualCellV)
            {
                // Outside cell band → pack total or split-pack encoding.
                return null;
            }

            cells.Add(volts);
        }

        foreach (var mv in voltagesExtMv)
        {
            if (mv == 0)
            {
                continue;
            }

            var volts = mv / 1000.0;
            if (volts < MinIndividualCellV || volts > MaxIndividualCellV)
            {
                return null;
            }

            cells.Add(volts);
        }

        return cells.Count >= 2 ? cells.ToArray() : null;
    }

    private static void ForwardFillKey(
        Dictionary<string, object> atMs,
        Dictionary<string, object> lastValues,
        string key)
    {
        if (atMs.TryGetValue(key, out var value))
        {
            lastValues[key] = value;
            return;
        }

        if (lastValues.TryGetValue(key, out var last))
        {
            atMs[key] = last;
        }
    }

    private static bool TryAsDouble(
        IReadOnlyDictionary<string, object> fields,
        string key,
        out double result)
    {
        result = 0;
        if (!fields.TryGetValue(key, out var value))
        {
            return false;
        }

        return value switch
        {
            double d when double.IsFinite(d) => Assign(d, out result),
            float f when float.IsFinite(f) => Assign(f, out result),
            int i => Assign(i, out result),
            long l => Assign(l, out result),
            _ => false
        };
    }

    private static bool Assign(double value, out double result)
    {
        result = value;
        return true;
    }
}
