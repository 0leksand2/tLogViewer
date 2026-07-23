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
        double trimSeconds = 30,
        bool splitIntoFlights = true)
    {
        if (messages.Count == 0)
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

        var heartbeats = VehicleHeartbeatSelector.SelectFromMessages(messages);
        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> flightIntervals;

        if (splitIntoFlights)
        {
            var margin = TimeSpan.FromSeconds(Math.Max(0, trimSeconds));
            var homeTimes = HomePositionTimeFinder.FindFromMessages(messages);
            flightIntervals = FlightSplitIntervalFinder.Find(
                heartbeats,
                homeTimes,
                logStart,
                logEnd,
                margin);

            if (flightIntervals.Count == 0)
            {
                return Array.Empty<FlightDto>();
            }
        }
        else
        {
            flightIntervals = [(logStart, logEnd)];
        }

        var flights = new List<FlightDto>(flightIntervals.Count);

        for (var i = 0; i < flightIntervals.Count; i++)
        {
            var (start, end) = flightIntervals[i];
            var armedWindow = FindArmedWindow(heartbeats, start, end);

            var byMillisecond = new Dictionary<long, Dictionary<string, object>>();
            var statusTextsByMs = new Dictionary<long, List<FlightStatusText>>();
            var messageCount = 0;

            foreach (var message in messages)
            {
                var time = TlogTime.ParseUtc(message.TimeUtc);
                if (time < start || time > end)
                {
                    continue;
                }

                var ms = time.ToUnixTimeMilliseconds();

                if (TryPushStatusText(statusTextsByMs, ms, message))
                {
                    messageCount++;
                    continue;
                }

                if (!byMillisecond.TryGetValue(ms, out var atMs))
                {
                    atMs = new Dictionary<string, object>(StringComparer.Ordinal);
                    byMillisecond[ms] = atMs;
                }

                if (BatteryFieldsEnricher.TryPush(atMs, message, out _))
                {
                    messageCount++;
                    continue;
                }

                if (SensorFieldsEnricher.TryPush(atMs, message))
                {
                    messageCount++;
                    continue;
                }

                PushFlattenedValues(atMs, message);
                messageCount++;
            }

            PlaneCoordinateEnricher.Enrich(byMillisecond);
            DerivedFieldsEnricher.ForwardFill(byMillisecond);
            BatteryFieldsEnricher.EstimateMissingCellsFromSysStatus(byMillisecond);
            BatteryFieldsEnricher.ForwardFill(byMillisecond);
            RemoveNonFiniteNumbers(byMillisecond);

            var homePoints = ExtractHomePoints(byMillisecond);
            if (homePoints.Count == 0)
            {
                homePoints = ExtractHomePoints(messages, start, end);
            }

            var modeChangePoints = ExtractModeChangePoints(byMillisecond);
            var armChangePoints = ExtractArmChangePoints(byMillisecond);

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
                HomePoints = homePoints,
                ModeChangePoints = modeChangePoints,
                ArmChangePoints = armChangePoints,
                StatusTexts = statusTextsByMs.ToDictionary(
                    static pair => pair.Key,
                    static pair => (IReadOnlyList<FlightStatusText>)pair.Value)
            });
        }

        return flights;
    }

    /// <summary>
    /// Stores STATUSTEXT on the parallel per-ms map (not in flattened telemetry).
    /// </summary>
    private static bool TryPushStatusText(
        Dictionary<long, List<FlightStatusText>> statusTextsByMs,
        long ms,
        MavMessageDto message)
    {
        if (!string.Equals(message.Type, "statusText", StringComparison.Ordinal)
            && !string.Equals(message.MessageId, "253", StringComparison.Ordinal))
        {
            return false;
        }

        if (message.Data is not StatusTextData data)
        {
            return false;
        }

        var text = data.Text?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            return true;
        }

        if (!statusTextsByMs.TryGetValue(ms, out var list))
        {
            list = [];
            statusTextsByMs[ms] = list;
        }

        list.Add(new FlightStatusText
        {
            Severity = data.Severity,
            Text = text
        });
        return true;
    }

    /// <summary>
    /// Flattens message payload into keys named <c>{messageId}_{fff}</c> (numeric field id).
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
            if (value is null || !IsJsonSafeValue(value))
            {
                continue;
            }

            var valueName = PropertyNaming.ConvertName(property.Name);
            atMs[FlightFieldIds.Format(message.MessageId, valueName)] = value;
        }
    }

    /// <summary>
    /// System.Text.Json cannot serialize float/double NaN or Infinity by default.
    /// Drop those keys so GetFlight responses stay valid (common in unsplit full logs).
    /// </summary>
    private static void RemoveNonFiniteNumbers(Dictionary<long, Dictionary<string, object>> byMillisecond)
    {
        foreach (var atMs in byMillisecond.Values)
        {
            List<string>? removeKeys = null;
            foreach (var (key, value) in atMs)
            {
                if (IsJsonSafeValue(value))
                {
                    continue;
                }

                (removeKeys ??= []).Add(key);
            }

            if (removeKeys is null)
            {
                continue;
            }

            foreach (var key in removeKeys)
            {
                atMs.Remove(key);
            }
        }
    }

    private static bool IsJsonSafeValue(object value) =>
        value switch
        {
            float f => float.IsFinite(f),
            double d => double.IsFinite(d),
            _ => true
        };

    /// <summary>
    /// Collects milliseconds where HEARTBEAT customMode differs from the previous value.
    /// </summary>
    private static List<FlightModeChangePoint> ExtractModeChangePoints(
        Dictionary<long, Dictionary<string, object>> byMillisecond)
    {
        var points = new List<FlightModeChangePoint>();
        uint? lastMode = null;

        foreach (var ms in byMillisecond.Keys.OrderBy(static key => key))
        {
            if (!TryReadCustomMode(byMillisecond[ms], out var mode))
            {
                continue;
            }

            if (lastMode.HasValue && mode != lastMode.Value)
            {
                points.Add(new FlightModeChangePoint
                {
                    ChangedAtMs = ms,
                    CustomMode = mode
                });
            }

            lastMode = mode;
        }

        return points;
    }

    /// <summary>
    /// Collects milliseconds where HEARTBEAT armed state differs from the previous value.
    /// </summary>
    private static List<FlightArmChangePoint> ExtractArmChangePoints(
        Dictionary<long, Dictionary<string, object>> byMillisecond)
    {
        var points = new List<FlightArmChangePoint>();
        bool? lastArmed = null;

        foreach (var ms in byMillisecond.Keys.OrderBy(static key => key))
        {
            if (!TryReadArmed(byMillisecond[ms], out var armed))
            {
                continue;
            }

            if (lastArmed.HasValue && armed != lastArmed.Value)
            {
                points.Add(new FlightArmChangePoint
                {
                    ChangedAtMs = ms,
                    Armed = armed
                });
            }

            lastArmed = armed;
        }

        return points;
    }

    private static bool TryReadArmed(IReadOnlyDictionary<string, object> fields, out bool armed)
    {
        armed = false;
        if (!fields.TryGetValue(FlightFieldIds.Armed, out var value))
        {
            return false;
        }

        switch (value)
        {
            case bool b:
                armed = b;
                return true;
            default:
                return false;
        }
    }

    private static bool TryReadCustomMode(IReadOnlyDictionary<string, object> fields, out uint mode)
    {
        mode = 0;
        if (!fields.TryGetValue(FlightFieldIds.CustomMode, out var value))
        {
            return false;
        }

        switch (value)
        {
            case uint u:
                mode = u;
                return true;
            case int i when i >= 0:
                mode = (uint)i;
                return true;
            case long l when l >= 0 && l <= uint.MaxValue:
                mode = (uint)l;
                return true;
            case ulong ul when ul <= uint.MaxValue:
                mode = (uint)ul;
                return true;
            case byte b:
                mode = b;
                return true;
            case short s when s >= 0:
                mode = (uint)s;
                return true;
            case ushort us:
                mode = us;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Builds home points from flattened <c>242_*</c> fields already placed in the flight buckets.
    /// </summary>
    private static List<FlightHomePoint> ExtractHomePoints(
        Dictionary<long, Dictionary<string, object>> byMillisecond)
    {
        var points = new List<FlightHomePoint>();
        double? lastLat = null;
        double? lastLng = null;

        foreach (var ms in byMillisecond.Keys.OrderBy(static key => key))
        {
            var fields = byMillisecond[ms];
            if (!TryReadHomeCoordinate(fields, out var latitudeDeg, out var longitudeDeg, out var altitudeM))
            {
                continue;
            }

            if (lastLat.HasValue && lastLng.HasValue
                && CoordinatesEqual(latitudeDeg, longitudeDeg, lastLat.Value, lastLng.Value))
            {
                continue;
            }

            points.Add(new FlightHomePoint
            {
                ChangedAtMs = ms,
                LatitudeDeg = latitudeDeg,
                LongitudeDeg = longitudeDeg,
                AltitudeM = altitudeM
            });

            lastLat = latitudeDeg;
            lastLng = longitudeDeg;
        }

        return points;
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
            if (!TryGetHomePositionData(message, out var home))
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

    private static bool TryGetHomePositionData(MavMessageDto message, out HomePositionData home)
    {
        if (message.Data is HomePositionData typed)
        {
            home = typed;
            return true;
        }

        home = null!;
        return false;
    }

    private static bool TryReadHomeCoordinate(
        IReadOnlyDictionary<string, object> fields,
        out double latitudeDeg,
        out double longitudeDeg,
        out double altitudeM)
    {
        latitudeDeg = 0;
        longitudeDeg = 0;
        altitudeM = 0;

        if (!fields.TryGetValue(FlightFieldIds.HomeLatitudeDeg, out var latObj)
            || !fields.TryGetValue(FlightFieldIds.HomeLongitudeDeg, out var lonObj))
        {
            return false;
        }

        if (!TryAsDouble(latObj, out latitudeDeg) || !TryAsDouble(lonObj, out longitudeDeg))
        {
            return false;
        }

        if (IsInvalidCoordinate(latitudeDeg, longitudeDeg))
        {
            return false;
        }

        if (fields.TryGetValue(FlightFieldIds.HomeAltitudeM, out var altObj))
        {
            TryAsDouble(altObj, out altitudeM);
        }

        return true;
    }

    private static bool TryAsDouble(object value, out double result)
    {
        switch (value)
        {
            case double d when double.IsFinite(d):
                result = d;
                return true;
            case float f when float.IsFinite(f):
                result = f;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case decimal m:
                result = (double)m;
                return true;
            default:
                result = 0;
                return false;
        }
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
