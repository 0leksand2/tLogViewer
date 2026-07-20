using tLogViewer.Core.Models;

namespace tLogViewer.Services;

public interface ITlogProcessingService
{
    TlogParseResult Process(Stream stream);
    TlogParseResult Process(string filePath);
}
