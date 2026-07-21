using tLogViewer.Core.Models;
using tLogViewer.Reader.Services;

namespace tLogViewer.Services.Services;

public sealed class TlogFlightSplitter
{
    private static readonly TimeSpan DefaultMargin = TimeSpan.FromSeconds(30);
    private const long DefaultMinSizeBytes = 100L * 1024 * 1024;

    public IReadOnlyList<string> SplitLargeLogs(
        string dataDirectory,
        long minSizeBytes = DefaultMinSizeBytes,
        TimeSpan? margin = null)
    {
        if (!Directory.Exists(dataDirectory))
        {
            throw new DirectoryNotFoundException($"Data directory not found: {dataDirectory}");
        }

        margin ??= DefaultMargin;
        var outputs = new List<string>();

        foreach (var sourcePath in Directory.EnumerateFiles(dataDirectory, "*.tlog"))
        {
            var fileName = Path.GetFileName(sourcePath);
            if (IsSplitOutput(fileName))
            {
                continue;
            }

            var info = new FileInfo(sourcePath);
            if (info.Length < minSizeBytes)
            {
                continue;
            }

            outputs.AddRange(SplitFile(sourcePath, margin.Value));
        }

        return outputs;
    }

    public IReadOnlyList<string> SplitFile(string sourcePath, TimeSpan margin)
    {
        var fileName = Path.GetFileName(sourcePath);
        Console.WriteLine($"Processing {fileName}...");

        var records = LogReader.ReadTLog(sourcePath).ToList();
        if (records.Count == 0)
        {
            return Array.Empty<string>();
        }

        var logStartUs = records[0].Trail;
        var logEndUs = records[^1].Trail;

        var heartbeats = VehicleHeartbeatSelector.SelectVehicleHeartbeats(records);
        var logStartUtc = ArmedIntervalFinder.TrailToUtc(logStartUs);
        var logEndUtc = ArmedIntervalFinder.TrailToUtc(logEndUs);
        var flightIntervals = FlightSplitIntervalFinder.Find(heartbeats, logStartUtc, logEndUtc, margin);

        if (flightIntervals.Count == 0)
        {
            Console.WriteLine($"No flight intervals in {fileName} — skipped.");
            return Array.Empty<string>();
        }

        var directory = Path.GetDirectoryName(sourcePath)!;
        var baseName = Path.GetFileNameWithoutExtension(sourcePath);
        var outputs = new List<string>(flightIntervals.Count);

        for (var i = 0; i < flightIntervals.Count; i++)
        {
            var (startUtc, endUtc) = flightIntervals[i];

            var startUs = ArmedIntervalFinder.UtcToTrail(startUtc);
            var endUs = ArmedIntervalFinder.UtcToTrail(endUtc);

            var outputPath = Path.Combine(directory, $"{baseName} flight {i + 1}.tlog");
            WriteSegment(records, startUs, endUs, outputPath);

            var written = new FileInfo(outputPath).Length;
            Console.WriteLine(
                $"Wrote {Path.GetFileName(outputPath)} ({FormatSize(written)}, {startUtc:u} → {endUtc:u})");

            outputs.Add(outputPath);
        }

        return outputs;
    }

    private static void WriteSegment(
        IReadOnlyList<TLogRecord> records,
        ulong startUs,
        ulong endUs,
        string outputPath)
    {
        using var stream = File.Create(outputPath);
        foreach (var record in records)
        {
            if (record.Trail < startUs || record.Trail > endUs)
            {
                continue;
            }

            TLogWriter.WriteRecord(stream, record.Trail, record.Packet);
        }
    }

    private static bool IsSplitOutput(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        return name.Contains(" flight ", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(name[(name.LastIndexOf(' ') + 1)..], out _);
    }

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a >= b ? a : b;

    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a <= b ? a : b;

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024)
        {
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        }

        if (bytes >= 1024 * 1024)
        {
            return $"{bytes / (1024.0 * 1024):F2} MB";
        }

        if (bytes >= 1024)
        {
            return $"{bytes / 1024.0:F2} KB";
        }

        return $"{bytes} B";
    }
}
