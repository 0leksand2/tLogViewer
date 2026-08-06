using tLogViewer.Core.Models;

namespace tLogViewer.Services.Interfaces;

/// <summary>
/// Caches fully analyzed TLog flights keyed by file name + vehicle system id (+ size).
/// Entries live for one hour so re-uploading the same log skips re-analysis.
/// </summary>
public interface IFlightAnalysisCache
{
    /// <summary>
    /// Builds a stable cache key from the upload identity.
    /// </summary>
    string BuildKey(string fileName, byte systemId, long size, bool splitIntoFlights);

    bool TryGet(string cacheKey, out TlogParseResult? result);

    void Set(string cacheKey, TlogParseResult result);

    int RemoveExpired(TimeSpan maxAge);
}
