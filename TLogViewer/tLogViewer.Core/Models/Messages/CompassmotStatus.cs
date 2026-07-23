namespace tLogViewer.Core.Models.Messages;

/// <summary>ArduPilot COMPASSMOT_STATUS (177).</summary>
public sealed class CompassmotStatus : MavLinkMessage
{
    public float CurrentA;
    public float CompensationX;
    public float CompensationY;
    public float CompensationZ;
    public ushort ThrottleDpct;
    public ushort InterferencePct;

    public override int ExpectedLength => 20;

    public CompassmotStatus(MavPacket packet) : base(packet)
    {
        CurrentA = BitConverter.ToSingle(FullPacket, 0);
        CompensationX = BitConverter.ToSingle(FullPacket, 4);
        CompensationY = BitConverter.ToSingle(FullPacket, 8);
        CompensationZ = BitConverter.ToSingle(FullPacket, 12);
        ThrottleDpct = BitConverter.ToUInt16(FullPacket, 16);
        InterferencePct = BitConverter.ToUInt16(FullPacket, 18);
    }

    public override void Print() =>
        Console.WriteLine(
            $"COMPASSMOT_STATUS: Current={CurrentA}A Interference={InterferencePct}%");
}
