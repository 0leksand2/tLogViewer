namespace tLogViewer.DTO.Messages;

public sealed class StatusTextData
{
    public byte Severity { get; init; }
    public required string Text { get; init; }
}
