namespace tLogViewer.Core.Models;

/// <summary>One armed segment with messages indexed by Unix millisecond.</summary>
public sealed class FlightDto
{
    public Guid Id { get; init; }
    public required string StartTimeUtc { get; init; }
    public required string EndTimeUtc { get; init; }
    public required string ArmedFromTimeUtc { get; init; }
    public required string ArmedUntilTimeUtc { get; init; }
    public double DurationSeconds { get; init; }
    public int MessageCount { get; init; }

    /// <summary>
    /// Outer key: Unix epoch millisecond.
    /// Inner key: <c>{messageId}_{valueName}</c> (e.g. <c>0_armed</c>); value: field value.
    /// </summary>
    public required IReadOnlyDictionary<long, IReadOnlyDictionary<string, object>> Messages { get; init; }
}
