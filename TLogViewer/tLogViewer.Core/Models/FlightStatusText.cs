namespace tLogViewer.Core.Models;

/// <summary>One STATUSTEXT line at a flight millisecond.</summary>
public sealed class FlightStatusText
{
    public byte Severity { get; init; }
    public required string Text { get; init; }
}
