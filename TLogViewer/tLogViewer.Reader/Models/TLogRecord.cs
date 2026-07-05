using System;
using System.Collections.Generic;
using System.Text;

namespace tLogViewer.Reader.Models
{
    public class TLogRecord
    {
        public ulong Trail { get; set; }

        public byte[] Packet { get; set; } = Array.Empty<byte>();
        public MavPacket MavPacket { get; set; }
    }
}
