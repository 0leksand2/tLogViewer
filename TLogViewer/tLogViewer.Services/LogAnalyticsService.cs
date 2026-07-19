using tLogViewer.Core.Models;
using tLogViewer.DTO.Messages;

namespace tLogViewer.Services;

public interface ILogAnalyticsService
{
    /// <summary>
    /// Splits parsed messages into flights based on Heartbeat SafetyArmed transitions.
    /// Each flight is trimmed by <paramref name="trimSeconds"/> before arm and after disarm.
    /// </summary>
    IReadOnlyList<FlightDto> SplitIntoFlights(
        IReadOnlyList<MavMessageDto> messages,
        double trimSeconds = 30);
}

public sealed class LogAnalyticsService : ILogAnalyticsService
{
    public IReadOnlyList<FlightDto> SplitIntoFlights(
        IReadOnlyList<MavMessageDto> messages,
        double trimSeconds = 30)
    {
        if (messages.Count == 0)
        {
            return Array.Empty<FlightDto>();
        }

        var margin = TimeSpan.FromSeconds(Math.Max(0, trimSeconds));
        var armedIntervals = FindArmedIntervals(messages);

        if (armedIntervals.Count == 0)
        {
            return Array.Empty<FlightDto>();
        }

        var logStart = DateTimeOffset.MaxValue;
        var logEnd = DateTimeOffset.MinValue;
        foreach (var message in messages)
        {
            var t = TlogTime.ParseUtc(message.TimeUtc);
            if (t < logStart) logStart = t;
            if (t > logEnd) logEnd = t;
        }

        var flights = new List<FlightDto>(armedIntervals.Count);

        for (var i = 0; i < armedIntervals.Count; i++)
        {
            var (armedFrom, armedUntil) = armedIntervals[i];
            var start = Clamp(armedFrom - margin, logStart, logEnd);
            var end = Clamp(armedUntil + margin, logStart, logEnd);

            if (start > end)
            {
                (start, end) = (end, start);
            }

            var flightMessages = new List<MavMessageDto>();
            foreach (var message in messages)
            {
                var time = TlogTime.ParseUtc(message.TimeUtc);
                if (time >= start && time <= end)
                {
                    flightMessages.Add(message);
                }
            }

            flights.Add(new FlightDto
            {
                Id = Guid.NewGuid(),
                StartTimeUtc = Format(start),
                EndTimeUtc = Format(end),
                ArmedFromTimeUtc = Format(armedFrom),
                ArmedUntilTimeUtc = Format(armedUntil),
                DurationSeconds = (end - start).TotalSeconds,
                MessageCount = flightMessages.Count,
                Messages = flightMessages
            });
        }

        return flights;
    }

    private static List<(DateTimeOffset ArmedFrom, DateTimeOffset ArmedUntil)> FindArmedIntervals(
        IReadOnlyList<MavMessageDto> messages)
    {
        var intervals = new List<(DateTimeOffset, DateTimeOffset)>();
        bool? wasArmed = null;
        DateTimeOffset? armedFrom = null;

        foreach (var message in messages)
        {
            if (message.Type != "heartbeat" || message.Data is not HeartbeatData heartbeat)
            {
                continue;
            }

            var time = TlogTime.ParseUtc(message.TimeUtc);

            if (wasArmed != true && heartbeat.Armed)
            {
                armedFrom = time;
            }
            else if (wasArmed == true && !heartbeat.Armed && armedFrom.HasValue)
            {
                intervals.Add((armedFrom.Value, time));
                armedFrom = null;
            }

            wasArmed = heartbeat.Armed;
        }

        if (armedFrom.HasValue)
        {
            var lastTime = TlogTime.ParseUtc(messages[^1].TimeUtc);
            intervals.Add((armedFrom.Value, lastTime));
        }

        return intervals;
    }

    private static DateTimeOffset Clamp(DateTimeOffset value, DateTimeOffset min, DateTimeOffset max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static string Format(DateTimeOffset value) =>
        value.ToUniversalTime().UtcDateTime.ToString("o");
}
