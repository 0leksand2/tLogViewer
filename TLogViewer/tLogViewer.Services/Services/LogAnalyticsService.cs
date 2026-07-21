using System.Reflection;
using System.Text.Json;
using tLogViewer.Core.Models;
using tLogViewer.DTO.Messages;
using tLogViewer.Services.Interfaces;

namespace tLogViewer.Services.Services;

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

        var logStart = DateTimeOffset.MaxValue;
        var logEnd = DateTimeOffset.MinValue;
        foreach (var message in messages)
        {
            var t = TlogTime.ParseUtc(message.TimeUtc);
            if (t < logStart) logStart = t;
            if (t > logEnd) logEnd = t;
        }

        var heartbeats = VehicleHeartbeatSelector.SelectFromMessages(messages);
        var flightIntervals = FlightSplitIntervalFinder.Find(heartbeats, logStart, logEnd, margin);

        if (flightIntervals.Count == 0)
        {
            return Array.Empty<FlightDto>();
        }

        var flights = new List<FlightDto>(flightIntervals.Count);

        for (var i = 0; i < flightIntervals.Count; i++)
        {
            var (start, end) = flightIntervals[i];
            var armedWindow = FindArmedWindow(heartbeats, start, end);

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

            PlaneCoordinateEnricher.Enrich(byMillisecond);

            var homePoints = ExtractHomePoints(messages, start, end);

            flights.Add(new FlightDto
            {
                Id = Guid.NewGuid(),
                StartTimeUtc = Format(start),
                EndTimeUtc = Format(end),
                ArmedFromTimeUtc = Format(armedWindow.From),
                ArmedUntilTimeUtc = Format(armedWindow.Until),
                DurationSeconds = (end - start).TotalSeconds,
                MessageCount = messageCount,
                Messages = byMillisecond.ToDictionary(
                    static pair => pair.Key,
                    static pair => (IReadOnlyDictionary<string, object>)pair.Value),
                HomePoints = homePoints
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

    private static List<FlightHomePoint> ExtractHomePoints(
        IReadOnlyList<MavMessageDto> messages,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        var points = new List<FlightHomePoint>();
        double? lastLat = null;
        double? lastLng = null;

        foreach (var message in messages)
        {
            if (message.Data is not HomePositionData home)
            {
                continue;
            }

            var time = TlogTime.ParseUtc(message.TimeUtc);
            if (time < start || time > end)
            {
                continue;
            }

            if (IsInvalidCoordinate(home.LatitudeDeg, home.LongitudeDeg))
            {
                continue;
            }

            if (lastLat.HasValue && lastLng.HasValue
                && CoordinatesEqual(home.LatitudeDeg, home.LongitudeDeg, lastLat.Value, lastLng.Value))
            {
                continue;
            }

            points.Add(new FlightHomePoint
            {
                ChangedAtMs = time.ToUnixTimeMilliseconds(),
                LatitudeDeg = home.LatitudeDeg,
                LongitudeDeg = home.LongitudeDeg,
                AltitudeM = home.AltitudeM
            });

            lastLat = home.LatitudeDeg;
            lastLng = home.LongitudeDeg;
        }

        return points;
    }

    private static (DateTimeOffset From, DateTimeOffset Until) FindArmedWindow(
        IReadOnlyList<VehicleHeartbeatSelector.HeartbeatSample> heartbeats,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        var inSegment = heartbeats
            .Where(sample => sample.Time >= start && sample.Time <= end)
            .Select(static sample => (sample.Time, sample.Armed))
            .ToList();

        if (inSegment.Count == 0)
        {
            return (start, end);
        }

        var armedIntervals = ArmedIntervalFinder.Find(inSegment);
        if (armedIntervals.Count == 0)
        {
            return (start, end);
        }

        var first = armedIntervals[0].ArmedFrom;
        var last = armedIntervals[^1].ArmedUntil;
        return (first, last);
    }

    private static bool IsInvalidCoordinate(double latitudeDeg, double longitudeDeg) =>
        Math.Abs(latitudeDeg) < 1e-9 && Math.Abs(longitudeDeg) < 1e-9;

    private static bool CoordinatesEqual(double lat1, double lng1, double lat2, double lng2) =>
        Math.Abs(lat1 - lat2) < 1e-7 && Math.Abs(lng1 - lng2) < 1e-7;

    private static string Format(DateTimeOffset value) =>
        value.ToUniversalTime().UtcDateTime.ToString("o");
}
