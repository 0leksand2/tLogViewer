using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using tLogViewer.Core.Models;
using tLogViewer.Services.Interfaces;

namespace tLogViewer.Services.Services;

/// <summary>
/// In-memory cache of analyzed flights. Key = unique number derived from
/// file name + MAVLink system id + size + split flag. TTL = 1 hour.
/// </summary>
public sealed class FlightAnalysisCache : IFlightAnalysisCache
{
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);

    public string BuildKey(string fileName, byte systemId, long size, bool splitIntoFlights)
    {
        var material =
            $"{NormalizeFileName(fileName)}|{systemId}|{size}|{(splitIntoFlights ? 1 : 0)}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        // 16 hex chars ≈ 64-bit unique number for this upload identity.
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }

    public bool TryGet(string cacheKey, out TlogParseResult? result)
    {
        result = null;
        if (!_entries.TryGetValue(cacheKey, out var entry))
        {
            return false;
        }

        if (IsExpired(entry))
        {
            _entries.TryRemove(cacheKey, out _);
            return false;
        }

        result = entry.Result;
        return true;
    }

    public void Set(string cacheKey, TlogParseResult result)
    {
        _entries[cacheKey] = new CacheEntry(DateTimeOffset.UtcNow, result);
    }

    public int RemoveExpired(TimeSpan maxAge)
    {
        var removed = 0;
        var cutoff = DateTimeOffset.UtcNow - maxAge;

        foreach (var pair in _entries)
        {
            if (pair.Value.AnalyzedAtUtc <= cutoff && _entries.TryRemove(pair.Key, out _))
            {
                removed++;
            }
        }

        return removed;
    }

    private static bool IsExpired(CacheEntry entry) =>
        DateTimeOffset.UtcNow - entry.AnalyzedAtUtc >= DefaultTtl;

    private static string NormalizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName.Trim());
        return string.IsNullOrEmpty(name) ? fileName.Trim() : name;
    }

    private sealed record CacheEntry(DateTimeOffset AnalyzedAtUtc, TlogParseResult Result);
}
