using tLogViewer.Core.Models;

namespace tLogViewer.Services.Interfaces;

public interface ITlogProcessingService
{
    TlogParseResult Process(Stream stream);
    TlogParseResult Process(string filePath);
}
