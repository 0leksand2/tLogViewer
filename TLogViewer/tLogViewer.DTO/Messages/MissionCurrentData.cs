namespace tLogViewer.DTO.Messages;

public sealed class MissionCurrentData
{
    public ushort Seq { get; init; }
    public ushort Total { get; init; }
    public byte MissionState { get; init; }
    public byte MissionMode { get; init; }
}
