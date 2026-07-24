namespace tLogViewer.Core.Models.Messages;

/// <summary>ArduPilot RPM (226).</summary>
public sealed class Rpm : MavLinkMessage
{
    public float Rpm1;
    public float Rpm2;

    public override int ExpectedLength => 8;

    public Rpm(MavPacket packet) : base(packet)
    {
        Rpm1 = BitConverter.ToSingle(FullPacket, 0);
        Rpm2 = BitConverter.ToSingle(FullPacket, 4);
    }

    public override void Print() =>
        Console.WriteLine($"RPM: Rpm1={Rpm1:F1}, Rpm2={Rpm2:F1}");
}
