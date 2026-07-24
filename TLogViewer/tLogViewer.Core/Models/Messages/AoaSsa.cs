namespace tLogViewer.Core.Models.Messages;

/// <summary>ArduPilot AOA_SSA (11020) — Angle of Attack and Side Slip Angle.</summary>
public sealed class AoaSsa : MavLinkMessage
{
    public ulong TimeUsec;
    /// <summary>deg; Angle of Attack.</summary>
    public float Aoa;
    /// <summary>deg; Side Slip Angle.</summary>
    public float Ssa;

    /// <summary>time_usec (8) + AOA (4) + SSA (4) = 16 bytes.</summary>
    public override int ExpectedLength => 16;

    public AoaSsa(MavPacket packet) : base(packet)
    {
        TimeUsec = BitConverter.ToUInt64(FullPacket, 0);
        Aoa = BitConverter.ToSingle(FullPacket, 8);
        Ssa = BitConverter.ToSingle(FullPacket, 12);
    }

    public override void Print() =>
        Console.WriteLine($"AOA_SSA: AOA={Aoa:F2}, SSA={Ssa:F2}");
}
