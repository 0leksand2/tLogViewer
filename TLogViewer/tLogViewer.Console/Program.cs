using tLogViewer.Core.Enums.Heartbeat;
using tLogViewer.Reader.Services;
using tLogViewer.Services.Services;

const long defaultMinSizeBytes = 100L * 1024 * 1024;

if (args.Length >= 1 && File.Exists(args[0]))
{
    AnalyzeAndSplitFile(args[0], TimeSpan.FromSeconds(30));
    return;
}

var dataDirectory = args.Length > 0
    ? args[0]
    : @"D:\projects\tLogViewer\data";

Console.WriteLine($"Splitting .tlog files over {defaultMinSizeBytes / (1024 * 1024)} MB in:");
Console.WriteLine($"  {dataDirectory}");
Console.WriteLine();

var splitter = new TlogFlightSplitter();
var outputs = splitter.SplitLargeLogs(dataDirectory, defaultMinSizeBytes);

Console.WriteLine();
Console.WriteLine(outputs.Count == 0
    ? "No files were split."
    : $"Created {outputs.Count} flight log(s).");

static void AnalyzeAndSplitFile(string sourcePath, TimeSpan margin)
{
    Console.WriteLine($"Analyzing {Path.GetFileName(sourcePath)}...");
    var records = LogReader.ReadTLog(sourcePath).ToList();
    if (records.Count == 0)
    {
        Console.WriteLine("Empty log.");
        return;
    }

    var logStart = ArmedIntervalFinder.TrailToUtc(records[0].Trail);
    var logEnd = ArmedIntervalFinder.TrailToUtc(records[^1].Trail);

    var heartbeats = VehicleHeartbeatSelector.SelectVehicleHeartbeats(records);
    var powerUps = PowerUpIntervalFinder.FindPowerUpEvents(
        heartbeats.Select(static s => (s.Time, s.SystemStatus)));
    var armedIntervals = ArmedIntervalFinder.Find(
        heartbeats.Select(static s => (s.Time, s.Armed)));

    Console.WriteLine($"Heartbeats (vehicle): {heartbeats.Count}");
    Console.WriteLine($"Power-up events: {powerUps.Count}");
    foreach (var powerUp in powerUps)
    {
        Console.WriteLine($"  {powerUp:u}");
    }

    Console.WriteLine($"Armed intervals: {armedIntervals.Count}");
    foreach (var (from, until) in armedIntervals)
    {
        Console.WriteLine($"  {from:u} → {until:u}");
    }

    var statusCounts = heartbeats
        .GroupBy(static s => s.SystemStatus)
        .OrderByDescending(static g => g.Count())
        .Select(static g => $"{g.Key}: {g.Count()}");
    Console.WriteLine("SystemStatus counts:");
    foreach (var line in statusCounts)
    {
        Console.WriteLine($"  {line}");
    }

    var flightIntervals = FlightSplitIntervalFinder.Find(heartbeats, logStart, logEnd, margin);

    Console.WriteLine($"Flight intervals: {flightIntervals.Count}");
    foreach (var (from, until) in flightIntervals)
    {
        Console.WriteLine($"  {from:u} → {until:u}");
    }

    Console.WriteLine();
    var splitter = new TlogFlightSplitter();
    var outputs = splitter.SplitFile(sourcePath, margin);
    Console.WriteLine($"Created {outputs.Count} flight log(s).");
}
