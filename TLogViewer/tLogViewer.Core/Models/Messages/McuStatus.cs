namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink MCU_STATUS (11039).</summary>
public sealed class McuStatus : MavLinkMessage
{
    /// <summary>cdegC.</summary>
    public short McuTemperatureCdegC;
    /// <summary>mV.</summary>
    public ushort McuVoltageMv;
    /// <summary>mV.</summary>
    public ushort McuVoltageMinMv;
    /// <summary>mV.</summary>
    public ushort McuVoltageMaxMv;
    /// <summary>MCU instance.</summary>
    public byte Id;

    public override int ExpectedLength => 9;

    public McuStatus(MavPacket packet) : base(packet)
    {
        McuTemperatureCdegC = BitConverter.ToInt16(FullPacket, 0);
        McuVoltageMv = BitConverter.ToUInt16(FullPacket, 2);
        McuVoltageMinMv = BitConverter.ToUInt16(FullPacket, 4);
        McuVoltageMaxMv = BitConverter.ToUInt16(FullPacket, 6);
        Id = FullPacket[8];
    }

    public override void Print() =>
        Console.WriteLine($"MCU_STATUS: Id={Id}, Temp={McuTemperatureCdegC / 100.0:F1}C, Vcc={McuVoltageMv}mV");
}
