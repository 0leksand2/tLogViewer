namespace tLogViewer.Core.Models;

public sealed class TlogParseResult
{
    public int TotalRecords { get; init; }
    public int ParsedCount { get; init; }
    public required IReadOnlyList<FlightDto> Flights { get; init; }
}
