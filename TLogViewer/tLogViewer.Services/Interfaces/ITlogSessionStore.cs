using tLogViewer.Core.Models;

namespace tLogViewer.Services.Interfaces;

public interface ITlogSessionStore
{
    string Store(string fileName, long size, TlogParseResult parseResult);

    TlogSessionSnapshot? GetSnapshot(string sessionId);

    /// <summary>
    /// Returns one flight for the client. Sessions remain until the 1-hour TTL
    /// (re-selecting a flight within the session still works).
    /// </summary>
    bool TryTakeFlight(string sessionId, Guid flightId, out FlightDto? flight, out bool sessionReleased);

    int RemoveExpired(TimeSpan maxAge);
}
