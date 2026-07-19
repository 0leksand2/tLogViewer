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
}

public sealed class TlogParseResult
{
    public int TotalRecords { get; init; }
    public int ParsedCount { get; init; }
    public required IReadOnlyList<FlightDto> Flights { get; init; }
}

/// <summary>Flight metadata without message payloads (returned on upload).</summary>
public sealed class FlightSummary
{
    public Guid Id { get; init; }
    public required string StartTimeUtc { get; init; }
    public required string EndTimeUtc { get; init; }
    public required string ArmedFromTimeUtc { get; init; }
    public required string ArmedUntilTimeUtc { get; init; }
    public double DurationSeconds { get; init; }
    public int MessageCount { get; init; }
}

/// <summary>One armed segment with full trimmed message list.</summary>
public sealed class FlightDto
{
    public Guid Id { get; init; }
    public required string StartTimeUtc { get; init; }
    public required string EndTimeUtc { get; init; }
    public required string ArmedFromTimeUtc { get; init; }
    public required string ArmedUntilTimeUtc { get; init; }
    public double DurationSeconds { get; init; }
    public int MessageCount { get; init; }
    public required IReadOnlyList<MavMessageDto> Messages { get; init; }
}

public sealed class TlogFlightResponse
{
    public required string SessionId { get; init; }
    public required FlightDto Flight { get; init; }
    /// <summary>True when this download completed the session and memory was released.</summary>
    public bool SessionReleased { get; init; }
}

public sealed class MavMessageDto
{
    public required string Type { get; init; }
    public required string MessageId { get; init; }
    public required string TimeUtc { get; init; }
    public required object Data { get; init; }
}
