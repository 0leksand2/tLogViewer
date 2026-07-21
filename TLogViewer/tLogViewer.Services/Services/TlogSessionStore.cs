using System.Collections.Concurrent;
using tLogViewer.Core.Models;

using tLogViewer.Services.Interfaces;

namespace tLogViewer.Services.Services;

public sealed class TlogSessionStore : ITlogSessionStore
{
    private readonly ConcurrentDictionary<string, SessionEntry> _sessions = new(StringComparer.Ordinal);

    public string Store(string fileName, long size, TlogParseResult parseResult)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var flights = parseResult.Flights.ToList();
        var summaries = flights.Select(ToSummary).ToList();

        var entry = new SessionEntry(
            sessionId,
            fileName,
            size,
            DateTimeOffset.UtcNow,
            parseResult.TotalRecords,
            parseResult.ParsedCount,
            flights,
            summaries);

        _sessions[sessionId] = entry;
        return sessionId;
    }

    public TlogSessionSnapshot? GetSnapshot(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var entry))
        {
            return null;
        }

        if (IsExpired(entry))
        {
            _sessions.TryRemove(sessionId, out _);
            return null;
        }

        return entry.ToSnapshot();
    }

    public bool TryTakeFlight(string sessionId, Guid flightId, out FlightDto? flight, out bool sessionReleased)
    {
        flight = null;
        sessionReleased = false;

        if (!_sessions.TryGetValue(sessionId, out var entry))
        {
            return false;
        }

        if (IsExpired(entry))
        {
            _sessions.TryRemove(sessionId, out _);
            return false;
        }

        flight = entry.Flights.FirstOrDefault(f => f.Id == flightId);
        if (flight is null)
        {
            return false;
        }

        sessionReleased = entry.MarkDownloaded(flightId);

        if (sessionReleased)
        {
            _sessions.TryRemove(sessionId, out _);
        }

        return true;
    }

    public int RemoveExpired(TimeSpan maxAge)
    {
        var removed = 0;
        var cutoff = DateTimeOffset.UtcNow - maxAge;

        foreach (var pair in _sessions)
        {
            if (pair.Value.CreatedAtUtc <= cutoff && _sessions.TryRemove(pair.Key, out _))
            {
                removed++;
            }
        }

        return removed;
    }

    private static bool IsExpired(SessionEntry entry) =>
        DateTimeOffset.UtcNow - entry.CreatedAtUtc >= TimeSpan.FromMinutes(30);

    private static FlightSummary ToSummary(FlightDto flight) => new()
    {
        Id = flight.Id,
        StartTimeUtc = flight.StartTimeUtc,
        EndTimeUtc = flight.EndTimeUtc,
        ArmedFromTimeUtc = flight.ArmedFromTimeUtc,
        ArmedUntilTimeUtc = flight.ArmedUntilTimeUtc,
        DurationSeconds = flight.DurationSeconds,
        MessageCount = flight.MessageCount
    };

    private sealed class SessionEntry
    {
        private readonly object _gate = new();
        private readonly HashSet<Guid> _downloaded = new();

        public SessionEntry(
            string sessionId,
            string fileName,
            long size,
            DateTimeOffset createdAtUtc,
            int totalRecords,
            int parsedCount,
            IReadOnlyList<FlightDto> flights,
            IReadOnlyList<FlightSummary> summaries)
        {
            SessionId = sessionId;
            FileName = fileName;
            Size = size;
            CreatedAtUtc = createdAtUtc;
            TotalRecords = totalRecords;
            ParsedCount = parsedCount;
            Flights = flights;
            Summaries = summaries;
        }

        public string SessionId { get; }
        public string FileName { get; }
        public long Size { get; }
        public DateTimeOffset CreatedAtUtc { get; }
        public int TotalRecords { get; }
        public int ParsedCount { get; }
        public IReadOnlyList<FlightDto> Flights { get; }
        public IReadOnlyList<FlightSummary> Summaries { get; }

        public bool MarkDownloaded(Guid flightId)
        {
            lock (_gate)
            {
                _downloaded.Add(flightId);
                return _downloaded.Count >= Flights.Count;
            }
        }

        public TlogSessionSnapshot ToSnapshot() => new()
        {
            SessionId = SessionId,
            FileName = FileName,
            Size = Size,
            CreatedAtUtc = CreatedAtUtc,
            TotalRecords = TotalRecords,
            ParsedCount = ParsedCount,
            Flights = Summaries
        };
    }
}
