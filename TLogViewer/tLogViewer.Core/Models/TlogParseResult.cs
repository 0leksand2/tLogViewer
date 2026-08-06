namespace tLogViewer.Core.Models;

public sealed class TlogParseResult
{
    public int TotalRecords { get; init; }
    public int ParsedCount { get; init; }

    /// <summary>
    /// Vehicle MAVLink system id taken from the first usable packet in the log (0 if none).
    /// </summary>
    public byte SystemId { get; init; }

    public required IReadOnlyList<FlightDto> Flights { get; init; }
}
