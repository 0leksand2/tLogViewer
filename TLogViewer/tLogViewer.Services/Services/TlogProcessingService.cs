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
            byte systemId = 0;

            foreach (var record in LogReader.ReadTLog(readable))
            {
                totalRecords++;
                derived.ObservePacket(record);

                if (systemId == 0 && IsUsableVehicleSysId(record.MavPacket.SysId))
                {
                    systemId = record.MavPacket.SysId;
                }

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
                SystemId = systemId,
                Flights = flights
            };
        }
        finally
        {
            buffer?.Dispose();
        }
    }

    public byte PeekSystemId(Stream stream)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("PeekSystemId requires a seekable stream.", nameof(stream));
        }

        var origin = stream.Position;
        try
        {
            foreach (var record in LogReader.ReadTLog(stream))
            {
                if (IsUsableVehicleSysId(record.MavPacket.SysId))
                {
                    return record.MavPacket.SysId;
                }
            }

            return 0;
        }
        finally
        {
            stream.Position = origin;
        }
    }

    /// <summary>Skip unset / GCS / broadcast system ids.</summary>
    internal static bool IsUsableVehicleSysId(byte sysId) =>
        sysId is not 0 and not 253 and not 255;
}
