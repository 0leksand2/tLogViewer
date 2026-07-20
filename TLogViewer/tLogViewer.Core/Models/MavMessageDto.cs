namespace tLogViewer.Core.Models;

public sealed class MavMessageDto
{
    public required string Type { get; init; }
    public required string MessageId { get; init; }
    public required string TimeUtc { get; init; }
    public required object Data { get; init; }
}
