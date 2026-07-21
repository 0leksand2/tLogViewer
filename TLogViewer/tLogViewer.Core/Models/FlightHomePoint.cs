namespace tLogViewer.Core.Models;

/// <summary>Home position at the moment coordinates changed during a flight.</summary>
public sealed class FlightHomePoint
{
    public long ChangedAtMs { get; init; }
    public double LatitudeDeg { get; init; }
    public double LongitudeDeg { get; init; }
    public double AltitudeM { get; init; }
}
