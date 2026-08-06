using tLogViewer.Services.Interfaces;
using tLogViewer.Services.Services;

namespace TLogViewer.Web;

/// <summary>
/// Evicts in-memory TLog analysis cache and download sessions older than 1 hour.
/// </summary>
public sealed class TlogSessionCleanupService : BackgroundService
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(1);
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly ITlogSessionStore _sessionStore;
    private readonly IFlightAnalysisCache _analysisCache;
    private readonly ILogger<TlogSessionCleanupService> _logger;

    public TlogSessionCleanupService(
        ITlogSessionStore sessionStore,
        IFlightAnalysisCache analysisCache,
        ILogger<TlogSessionCleanupService> logger)
    {
        _sessionStore = sessionStore;
        _analysisCache = analysisCache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var sessionsRemoved = _sessionStore.RemoveExpired(MaxAge);
                var analysesRemoved = _analysisCache.RemoveExpired(MaxAge);

                if (sessionsRemoved > 0 || analysesRemoved > 0)
                {
                    _logger.LogInformation(
                        "Removed {Sessions} expired TLog session(s) and {Analyses} analysis cache entr(y/ies).",
                        sessionsRemoved,
                        analysesRemoved);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to purge expired TLog sessions / analysis cache.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
