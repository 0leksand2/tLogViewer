using tLogViewer.Core.Models;
using tLogViewer.Core.Models.Messages;
using tLogViewer.Reader.Services;

using tLogViewer.Services.Interfaces;

namespace tLogViewer.Services.Services;

public sealed class TlogProcessingService : ITlogProcessingService
{
    private readonly ILogAnalyticsService _analytics;

    public TlogProcessingService(ILogAnalyticsService analytics)
    {
        _analytics = analytics;
    }

    public TlogParseResult Process(string filePath, bool splitIntoFlights = true)
    {
        using var stream = File.OpenRead(filePath);
        return Process(stream, splitIntoFlights);
    }

    public TlogParseResult Process(Stream stream, bool splitIntoFlights = true)
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
            var derived = new DerivedMessageCalculator();
            var totalRecords = 0;

            foreach (var record in LogReader.ReadTLog(readable))
            {
                totalRecords++;
                derived.ObservePacket(record);

                var parsed = MessageProcessingFactory.ParseMessage(record.MavPacket);
                if (parsed is null)
                {
                    continue;
                }

                if (parsed is Heartbeat heartbeat)
                {
                    derived.ObserveHeartbeat(record, heartbeat);
                }

                messages.Add(MavMessageMapper.ToDto(parsed, record.Trail));
            }

            messages.AddRange(derived.TakeSamples());

            var flights = _analytics.SplitIntoFlights(messages, splitIntoFlights: splitIntoFlights);

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
