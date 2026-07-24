namespace tLogViewer.Core.Models.Messages;

/// <summary>ArduPilot RANGEFINDER (173).</summary>
public sealed class Rangefinder : MavLinkMessage
{
    public float DistanceM;
    public float VoltageV;

    public override int ExpectedLength => 8;

    public Rangefinder(MavPacket packet) : base(packet)
    {
        DistanceM = BitConverter.ToSingle(FullPacket, 0);
        VoltageV = BitConverter.ToSingle(FullPacket, 4);
    }

    public override void Print() =>
        Console.WriteLine($"RANGEFINDER: Distance={DistanceM:F2}m, Voltage={VoltageV:F2}V");
}
