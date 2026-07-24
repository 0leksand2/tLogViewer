namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink HWSTATUS (165) — deprecated, superseded by POWER_STATUS.</summary>
public sealed class HwStatus : MavLinkMessage
{
    public ushort VccMv;
    public byte I2Cerr;

    public override int ExpectedLength => 3;

    public HwStatus(MavPacket packet) : base(packet)
    {
        VccMv = BitConverter.ToUInt16(FullPacket, 0);
        I2Cerr = FullPacket[2];
    }

    public override void Print() =>
        Console.WriteLine($"HWSTATUS: Vcc={VccMv}mV, I2Cerr={I2Cerr}");
}
