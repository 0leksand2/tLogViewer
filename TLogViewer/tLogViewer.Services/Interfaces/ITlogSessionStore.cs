using tLogViewer.Core.Models;

namespace tLogViewer.Services.Interfaces;

public interface ITlogSessionStore
{
    string Store(string fileName, long size, TlogParseResult parseResult);

    TlogSessionSnapshot? GetSnapshot(string sessionId);

    /// <summary>
    /// Takes one flight for the client. Marks it downloaded; removes the session
    /// when every flight has been downloaded.
    /// </summary>
    bool TryTakeFlight(string sessionId, Guid flightId, out FlightDto? flight, out bool sessionReleased);

    int RemoveExpired(TimeSpan maxAge);
}
