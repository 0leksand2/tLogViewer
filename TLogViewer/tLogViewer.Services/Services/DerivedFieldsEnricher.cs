namespace tLogViewer.Services.Services;

/// <summary>
/// Fills DERIVED (998) fields on every flight millisecond:
/// link quality is forward-filled from 1 Hz samples;
/// time-since-arm is computed at each millisecond from heartbeat armed state.
/// </summary>
public static class DerivedFieldsEnricher
{
    private const string LinkQualityKey = "998_linkQualityGcs";
    private const string TimeSinceArmKey = "998_timeSinceArmSec";
    private const string ArmedKey = "0_armed";

    public static void ForwardFill(Dictionary<long, Dictionary<string, object>> byMillisecond)
    {
        if (byMillisecond.Count == 0)
        {
            return;
        }

        object? lastLinkQuality = null;
        bool? armed = null;
        long? armedFromMs = null;

        foreach (var ms in byMillisecond.Keys.OrderBy(static key => key))
        {
            var atMs = byMillisecond[ms];

            if (atMs.TryGetValue(LinkQualityKey, out var linkQuality))
            {
                lastLinkQuality = linkQuality;
            }
            else if (lastLinkQuality is not null)
            {
                atMs[LinkQualityKey] = lastLinkQuality;
            }

            if (atMs.TryGetValue(ArmedKey, out var armedObj) && armedObj is bool isArmed)
            {
                if (isArmed && armed != true)
                {
                    armedFromMs = ms;
                }
                else if (!isArmed)
                {
                    armedFromMs = null;
                }

                armed = isArmed;
            }

            double timeSinceArmSec = 0;
            if (armed == true && armedFromMs.HasValue)
            {
                timeSinceArmSec = Math.Max(0, (ms - armedFromMs.Value) / 1000.0);
            }

            atMs[TimeSinceArmKey] = timeSinceArmSec;
        }
    }
}
