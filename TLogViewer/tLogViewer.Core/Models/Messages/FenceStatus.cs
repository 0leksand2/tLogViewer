namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink FENCE_STATUS (162).</summary>
public sealed class FenceStatus : MavLinkMessage
{
    public uint BreachTimeMs;
    public ushort BreachCount;
    /// <summary>0 if currently inside fence, 1 if outside.</summary>
    public byte BreachStatus;
    /// <summary>FENCE_BREACH.</summary>
    public byte BreachType;

    /// <summary>Base 8; breach_mitigation extension ignored.</summary>
    public override int ExpectedLength => 8;

    public FenceStatus(MavPacket packet) : base(packet)
    {
        BreachTimeMs = BitConverter.ToUInt32(FullPacket, 0);
        BreachCount = BitConverter.ToUInt16(FullPacket, 4);
        BreachStatus = FullPacket[6];
        BreachType = FullPacket[7];
    }

    public override void Print() =>
        Console.WriteLine($"FENCE_STATUS: Status={BreachStatus}, Count={BreachCount}, Type={BreachType}");
}
