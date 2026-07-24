namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink MEMINFO (152).</summary>
public sealed class MemInfo : MavLinkMessage
{
    public ushort Brkval;
    public ushort FreememBytes;
    /// <summary>bytes; extension, null when payload shorter than 8 bytes.</summary>
    public uint? Freemem32Bytes;

    /// <summary>Base 4; freemem32 extension → 8.</summary>
    public override int ExpectedLength => 8;

    public MemInfo(MavPacket packet) : base(packet)
    {
        Brkval = BitConverter.ToUInt16(FullPacket, 0);
        FreememBytes = BitConverter.ToUInt16(FullPacket, 2);

        if (packet.Payload.Length >= 8)
        {
            Freemem32Bytes = BitConverter.ToUInt32(FullPacket, 4);
        }
    }

    public override void Print() =>
        Console.WriteLine($"MEMINFO: Brkval={Brkval}, Freemem={FreememBytes}bytes, Freemem32={Freemem32Bytes}");
}
