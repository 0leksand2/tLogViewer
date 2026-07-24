namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink ESC_STATUS (291) — higher-rate ESC rpm/voltage/current.</summary>
public sealed class EscStatus : MavLinkMessage
{
    public ulong TimeUsec;
    public int[] Rpm = new int[4];
    public float[] VoltageV = new float[4];
    public float[] CurrentA = new float[4];
    /// <summary>0-based index of the first ESC in this message (0, 4, 8, …).</summary>
    public byte Index;

    public override int ExpectedLength => 57;

    public EscStatus(MavPacket packet) : base(packet)
    {
        TimeUsec = BitConverter.ToUInt64(FullPacket, 0);
        for (var i = 0; i < 4; i++)
        {
            Rpm[i] = BitConverter.ToInt32(FullPacket, 8 + i * 4);
            VoltageV[i] = BitConverter.ToSingle(FullPacket, 24 + i * 4);
            CurrentA[i] = BitConverter.ToSingle(FullPacket, 40 + i * 4);
        }

        Index = FullPacket[56];
    }

    public override void Print() =>
        Console.WriteLine($"ESC_STATUS: Index={Index}, Rpm=[{string.Join(",", Rpm)}]");
}
