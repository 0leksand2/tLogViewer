namespace tLogViewer.Core.Models
{
    public class TLogRecord
    {
        public ulong Trail { get; set; }

        public byte[] Packet { get; set; } = Array.Empty<byte>();
        public MavPacket MavPacket { get; set; }
    }
}
