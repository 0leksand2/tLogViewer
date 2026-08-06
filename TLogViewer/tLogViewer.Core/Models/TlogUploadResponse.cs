namespace tLogViewer.Core.Models;

public sealed class TlogUploadResponse
{
    public required string SessionId { get; init; }
    public required string FileName { get; init; }
    public long Size { get; init; }
    public int TotalRecords { get; init; }
    public int ParsedCount { get; init; }
    public int FlightCount { get; init; }
    public required IReadOnlyList<FlightSummary> Flights { get; init; }

    /// <summary>Vehicle MAVLink system id used for the analysis cache key.</summary>
    public byte SystemId { get; init; }

    /// <summary>True when flights were served from the 1-hour analysis cache.</summary>
    public bool FromCache { get; init; }

    /// <summary>Unique cache identity derived from file name + system id + size.</summary>
    public required string CacheKey { get; init; }
}
