using tLogViewer.Services;

namespace TLogViewer.Web;

/// <summary>
/// Evicts in-memory TLog sessions older than 30 minutes.
/// </summary>
public sealed class TlogSessionCleanupService : BackgroundService
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly ITlogSessionStore _store;
    private readonly ILogger<TlogSessionCleanupService> _logger;

    public TlogSessionCleanupService(ITlogSessionStore store, ILogger<TlogSessionCleanupService> logger)
    {
        _store = store;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var removed = _store.RemoveExpired(MaxAge);
                if (removed > 0)
                {
                    _logger.LogInformation("Removed {Count} expired TLog session(s) from memory.", removed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to purge expired TLog sessions.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
