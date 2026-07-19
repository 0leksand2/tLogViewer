using tLogViewer.Core.Models;
using tLogViewer.Reader.Services;

namespace tLogViewer.Services;

public interface ITlogProcessingService
{
    TlogParseResult Process(Stream stream);
    TlogParseResult Process(string filePath);
}

public sealed class TlogProcessingService : ITlogProcessingService
{
    private readonly ILogAnalyticsService _analytics;

    public TlogProcessingService(ILogAnalyticsService analytics)
    {
        _analytics = analytics;
    }

    public TlogParseResult Process(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return Process(stream);
    }

    public TlogParseResult Process(Stream stream)
    {
        Stream readable = stream;
        MemoryStream? buffer = null;

        if (!stream.CanSeek)
        {
            buffer = new MemoryStream();
            stream.CopyTo(buffer);
            buffer.Position = 0;
            readable = buffer;
        }

        try
        {
            var messages = new List<MavMessageDto>();
            var totalRecords = 0;

            foreach (var record in LogReader.ReadTLog(readable))
            {
                totalRecords++;

                var parsed = MessageProcessingFactory.ParseMessage(record.MavPacket);
                if (parsed is null)
                {
                    continue;
                }

                messages.Add(MavMessageMapper.ToDto(parsed, record.Trail));
            }

            var flights = _analytics.SplitIntoFlights(messages);

            return new TlogParseResult
            {
                TotalRecords = totalRecords,
                ParsedCount = messages.Count,
                Flights = flights
            };
        }
        finally
        {
            buffer?.Dispose();
        }
    }
}
