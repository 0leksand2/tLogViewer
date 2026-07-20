namespace tLogViewer.Core.Models;

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
