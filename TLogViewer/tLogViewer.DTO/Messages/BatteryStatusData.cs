namespace tLogViewer.DTO.Messages;

/// <summary>Parsed BATTERY_STATUS (147) for flight enrichment.</summary>
public sealed class BatteryStatusData
{
    public byte Id { get; init; }
    public sbyte BatteryRemainingPct { get; init; }
    /// <summary>Cell / pack voltage entries in millivolts (UINT16_MAX = unused).</summary>
    public required ushort[] VoltagesMv { get; init; }
    /// <summary>Extended cells 11–14 in millivolts (0 = unused / unsupported).</summary>
    public required ushort[] VoltagesExtMv { get; init; }
    public double? CurrentBatteryA { get; init; }
    public double? TemperatureC { get; init; }
    public int? CurrentConsumedMah { get; init; }
    /// <summary>Estimated seconds remaining; null when unknown.</summary>
    public int? TimeRemainingSec { get; init; }
}
