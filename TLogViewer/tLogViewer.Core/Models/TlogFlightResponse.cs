namespace tLogViewer.Core.Models;

public sealed class TlogFlightResponse
{
    public required string SessionId { get; init; }
    public required FlightDto Flight { get; init; }
    /// <summary>True when this download completed the session and memory was released.</summary>
    public bool SessionReleased { get; init; }
}
