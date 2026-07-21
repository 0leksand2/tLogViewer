using tLogViewer.Core.Enums;
using tLogViewer.Core.Models;
using tLogViewer.DTO.Messages;
using tLogViewer.Reader.Services;

namespace tLogViewer.Services.Services;

public static class HomePositionTimeFinder
{
    public static List<DateTimeOffset> FindFromRecords(IEnumerable<TLogRecord> records)
    {
        var times = new List<DateTimeOffset>();

        foreach (var record in records)
        {
            if (record.MavPacket.MsgId != MavMessageTypeId.HOME_POSITION)
            {
                continue;
            }

            times.Add(ArmedIntervalFinder.TrailToUtc(record.Trail));
        }

        return times;
    }

    public static List<DateTimeOffset> FindFromMessages(IEnumerable<MavMessageDto> messages)
    {
        var times = new List<DateTimeOffset>();

        foreach (var message in messages)
        {
            if (message.Data is not HomePositionData)
            {
                continue;
            }

            times.Add(TlogTime.ParseUtc(message.TimeUtc));
        }

        return times;
    }
}
