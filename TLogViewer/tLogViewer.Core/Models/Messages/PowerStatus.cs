namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink POWER_STATUS (125).</summary>
public sealed class PowerStatus : MavLinkMessage
{
    public ushort VccMv;
    public ushort VservoMv;
    /// <summary>Bitmap of MAV_POWER_STATUS flags.</summary>
    public ushort Flags;

    public override int ExpectedLength => 6;

    public PowerStatus(MavPacket packet) : base(packet)
    {
        VccMv = BitConverter.ToUInt16(FullPacket, 0);
        VservoMv = BitConverter.ToUInt16(FullPacket, 2);
        Flags = BitConverter.ToUInt16(FullPacket, 4);
    }

    public override void Print() =>
        Console.WriteLine($"POWER_STATUS: Vcc={VccMv}mV, Vservo={VservoMv}mV, Flags={Flags}");
}
