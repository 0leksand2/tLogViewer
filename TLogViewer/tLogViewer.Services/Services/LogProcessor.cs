using tLogViewer.Core.Models;
using tLogViewer.Reader.Services;

namespace tLogViewer.Services.Services;

public class LogProcessor
{
    public List<MavLinkMessage> ProcessLogFile(string path)
    {
        var messages = new List<MavLinkMessage>();
        foreach (var record in LogReader.ReadTLog(path))
        {
            var message = MessageProcessingFactory.ParseMessage(record.MavPacket);
            if (message != null)
                messages.Add(message);
        }
        return messages;
    }
}
