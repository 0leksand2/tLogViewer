namespace tLogViewer.DTO.Messages;

public sealed class RadioData
{
    public byte Rssi { get; init; }
    public byte RemRssi { get; init; }
    public byte TxBufPct { get; init; }
    public byte Noise { get; init; }
    public byte RemNoise { get; init; }
    public ushort RxErrors { get; init; }
    public ushort Fixed { get; init; }
}
