using tLogViewer.Core.Models;

namespace tLogViewer.Services;

public interface ILogAnalyticsService
{
    /// <summary>
    /// Splits parsed messages into flights based on Heartbeat SafetyArmed transitions.
    /// Each flight is trimmed by <paramref name="trimSeconds"/> before arm and after disarm.
    /// </summary>
    IReadOnlyList<FlightDto> SplitIntoFlights(
        IReadOnlyList<MavMessageDto> messages,
        double trimSeconds = 30);
}
