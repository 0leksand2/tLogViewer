using tLogViewer.Core.Models;

namespace tLogViewer.Services.Interfaces;

public interface ITlogProcessingService
{
    TlogParseResult Process(Stream stream, bool splitIntoFlights = true);

    TlogParseResult Process(string filePath, bool splitIntoFlights = true);

    /// <summary>
    /// Scans a seekable TLog stream for the first usable vehicle <c>SysId</c>
    /// (skips 0 / GCS 253 / 255). Rewinds the stream to its original position.
    /// </summary>
    byte PeekSystemId(Stream stream);
}
