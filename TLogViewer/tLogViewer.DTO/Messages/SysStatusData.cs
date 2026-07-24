namespace tLogViewer.DTO.Messages;

public sealed class SysStatusData
{
    public ushort Load { get; init; }
    public double BatteryVoltageV { get; init; }
    public double BatteryCurrentA { get; init; }
    public sbyte BatteryRemainingPct { get; init; }
    public ushort DropRateComm { get; init; }
    public ushort ErrorsComm { get; init; }
    public ushort Errors1 { get; init; }
    public ushort Errors2 { get; init; }
    public ushort Errors3 { get; init; }
    public ushort Errors4 { get; init; }
    public bool GpsPresent { get; init; }
    public bool GpsEnabled { get; init; }
    public bool GpsHealthy { get; init; }
    public bool TerrainActive { get; init; }
    public bool SafetyActive { get; init; }
    public bool PrearmStatus { get; init; }
}
