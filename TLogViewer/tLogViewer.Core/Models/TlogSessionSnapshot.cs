namespace tLogViewer.Core.Models;

public sealed class TlogSessionSnapshot
{
    public required string SessionId { get; init; }
    public required string FileName { get; init; }
    public long Size { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public int TotalRecords { get; init; }
    public int ParsedCount { get; init; }
    public required IReadOnlyList<FlightSummary> Flights { get; init; }
}
