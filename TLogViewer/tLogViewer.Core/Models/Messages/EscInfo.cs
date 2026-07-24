namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink ESC_INFO (290) — lower-rate ESC info; temperature in cdegC.</summary>
public sealed class EscInfo : MavLinkMessage
{
    public ulong TimeUsec;
    public uint[] ErrorCount = new uint[4];
    public ushort Counter;
    public ushort[] FailureFlags = new ushort[4];
    /// <summary>cdegC. INT16_MAX means not supplied.</summary>
    public short[] TemperatureCdegC = new short[4];
    /// <summary>0-based index of the first ESC in this message (0, 4, 8, …).</summary>
    public byte Index;
    /// <summary>Total ESC count across all ESC_INFO messages; slots at absolute index ≥ Count are invalid.</summary>
    public byte Count;
    public byte ConnectionType;
    public byte Info;

    public override int ExpectedLength => 46;

    public EscInfo(MavPacket packet) : base(packet)
    {
        TimeUsec = BitConverter.ToUInt64(FullPacket, 0);
        for (var i = 0; i < 4; i++)
        {
            ErrorCount[i] = BitConverter.ToUInt32(FullPacket, 8 + i * 4);
        }

        Counter = BitConverter.ToUInt16(FullPacket, 24);
        for (var i = 0; i < 4; i++)
        {
            FailureFlags[i] = BitConverter.ToUInt16(FullPacket, 26 + i * 2);
            TemperatureCdegC[i] = BitConverter.ToInt16(FullPacket, 34 + i * 2);
        }

        Index = FullPacket[42];
        Count = FullPacket[43];
        ConnectionType = FullPacket[44];
        Info = FullPacket[45];
    }

    public override void Print() =>
        Console.WriteLine($"ESC_INFO: Index={Index}, Count={Count}, Temp=[{string.Join(",", TemperatureCdegC)}]");
}
