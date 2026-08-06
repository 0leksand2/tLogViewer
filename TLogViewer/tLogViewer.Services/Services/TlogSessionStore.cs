using System.Collections.Concurrent;
using tLogViewer.Core.Models;
using tLogViewer.Services.Interfaces;

namespace tLogViewer.Services.Services;

public sealed class TlogSessionStore : ITlogSessionStore
{
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);

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
            parseResult.SystemId,
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
        // Sessions stay until TTL so re-selecting a flight still works; analysis cache
        // independently keeps the same log for re-upload within one hour.
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
        return flight is not null;
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
        DateTimeOffset.UtcNow - entry.CreatedAtUtc >= DefaultTtl;

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
        public SessionEntry(
            string sessionId,
            string fileName,
            long size,
            DateTimeOffset createdAtUtc,
            int totalRecords,
            int parsedCount,
            byte systemId,
            IReadOnlyList<FlightDto> flights,
            IReadOnlyList<FlightSummary> summaries)
        {
            SessionId = sessionId;
            FileName = fileName;
            Size = size;
            CreatedAtUtc = createdAtUtc;
            TotalRecords = totalRecords;
            ParsedCount = parsedCount;
            SystemId = systemId;
            Flights = flights;
            Summaries = summaries;
        }

        public string SessionId { get; }
        public string FileName { get; }
        public long Size { get; }
        public DateTimeOffset CreatedAtUtc { get; }
        public int TotalRecords { get; }
        public int ParsedCount { get; }
        public byte SystemId { get; }
        public IReadOnlyList<FlightDto> Flights { get; }
        public IReadOnlyList<FlightSummary> Summaries { get; }

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
