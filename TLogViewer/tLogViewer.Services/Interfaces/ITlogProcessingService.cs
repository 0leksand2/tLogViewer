using tLogViewer.Core.Models;

namespace tLogViewer.Services.Interfaces;

public interface ITlogProcessingService
{
    TlogParseResult Process(Stream stream, bool splitIntoFlights = true);
    TlogParseResult Process(string filePath, bool splitIntoFlights = true);
}
