using System.Text;
using tLogViewer.Core.Enums;

namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink STATUSTEXT (253) — human-readable status / log line.</summary>
public sealed class StatusText : MavLinkMessage
{
    public MavSeverity Severity;
    public string Text = string.Empty;

    /// <summary>severity + text[50]. MAVLink2 may append id/chunk fields beyond this.</summary>
    public override int ExpectedLength => 51;

    public StatusText(MavPacket packet) : base(packet)
    {
        Severity = (MavSeverity)FullPacket[0];
        Text = Encoding.ASCII.GetString(FullPacket, 1, 50).TrimEnd('\0').Trim();
    }

    public override void Print()
    {
        Console.WriteLine($"STATUSTEXT: [{Severity}] {Text}");
    }
}
