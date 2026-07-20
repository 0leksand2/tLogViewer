using System.Reflection;
using System.Text.Json;
using tLogViewer.Core.Models;
using tLogViewer.DTO.Messages;

namespace tLogViewer.Services;

public sealed class LogAnalyticsService : ILogAnalyticsService
{
    private static readonly JsonNamingPolicy PropertyNaming = JsonNamingPolicy.CamelCase;

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

            var byMillisecond = new Dictionary<long, Dictionary<string, object>>();
            var messageCount = 0;

            foreach (var message in messages)
            {
                var time = TlogTime.ParseUtc(message.TimeUtc);
                if (time < start || time > end)
                {
                    continue;
                }

                var ms = time.ToUnixTimeMilliseconds();
                if (!byMillisecond.TryGetValue(ms, out var atMs))
                {
                    atMs = new Dictionary<string, object>(StringComparer.Ordinal);
                    byMillisecond[ms] = atMs;
                }

                PushFlattenedValues(atMs, message);
                messageCount++;
            }

            flights.Add(new FlightDto
            {
                Id = Guid.NewGuid(),
                StartTimeUtc = Format(start),
                EndTimeUtc = Format(end),
                ArmedFromTimeUtc = Format(armedFrom),
                ArmedUntilTimeUtc = Format(armedUntil),
                DurationSeconds = (end - start).TotalSeconds,
                MessageCount = messageCount,
                Messages = byMillisecond.ToDictionary(
                    static pair => pair.Key,
                    static pair => (IReadOnlyDictionary<string, object>)pair.Value)
            });
        }

        return flights;
    }

    /// <summary>
    /// Flattens message payload into keys named <c>{messageId}_{valueName}</c>.
    /// </summary>
    private static void PushFlattenedValues(Dictionary<string, object> atMs, MavMessageDto message)
    {
        foreach (var property in message.Data.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var value = property.GetValue(message.Data);
            if (value is null)
            {
                continue;
            }

            var valueName = PropertyNaming.ConvertName(property.Name);
            atMs[$"{message.MessageId}_{valueName}"] = value;
        }
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
