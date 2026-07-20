namespace tLogViewer.DTO.Messages;

public sealed class RcChannelsData
{
    public uint TimeBootMs { get; init; }
    public byte ChannelCount { get; init; }
    public required IReadOnlyList<double> Channels { get; init; }
    public byte Rssi { get; init; }
}
