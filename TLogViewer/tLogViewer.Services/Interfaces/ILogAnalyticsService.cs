using tLogViewer.Core.Models;

namespace tLogViewer.Services.Interfaces;

public interface ILogAnalyticsService
{
    /// <summary>
    /// Prefers power-up (heartbeat UNINIT/BOOT); falls back to armed/disarmed when no boot markers exist.
    /// Each flight is trimmed by <paramref name="trimSeconds"/> at segment boundaries.
    /// </summary>
    IReadOnlyList<FlightDto> SplitIntoFlights(
        IReadOnlyList<MavMessageDto> messages,
        double trimSeconds = 30,
        bool splitIntoFlights = true);
}
