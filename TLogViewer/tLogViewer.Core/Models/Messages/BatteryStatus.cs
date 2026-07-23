namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink BATTERY_STATUS (147).</summary>
public sealed class BatteryStatus : MavLinkMessage
{
    public const ushort UnusedCellMv = ushort.MaxValue;

    public int CurrentConsumedMah;
    public int EnergyConsumedHJ;
    public short TemperatureCdegC;
    public ushort[] VoltagesMv = new ushort[10];
    public short CurrentBatteryCA;
    public byte Id;
    public byte BatteryFunction;
    public byte Type;
    public sbyte BatteryRemainingPct;
    public ushort[] VoltagesExtMv = new ushort[4];

    /// <summary>Base 36 bytes; MAVLink2 extensions through voltages_ext (49+) zero-padded.</summary>
    public override int ExpectedLength => 49;

    public BatteryStatus(MavPacket packet) : base(packet)
    {
        CurrentConsumedMah = BitConverter.ToInt32(FullPacket, 0);
        EnergyConsumedHJ = BitConverter.ToInt32(FullPacket, 4);
        TemperatureCdegC = BitConverter.ToInt16(FullPacket, 8);

        for (var i = 0; i < 10; i++)
        {
            VoltagesMv[i] = BitConverter.ToUInt16(FullPacket, 10 + i * 2);
        }

        CurrentBatteryCA = BitConverter.ToInt16(FullPacket, 30);
        Id = FullPacket[32];
        BatteryFunction = FullPacket[33];
        Type = FullPacket[34];
        BatteryRemainingPct = unchecked((sbyte)FullPacket[35]);

        // voltages_ext starts at offset 41 when charge_state (1 byte) + time_remaining (4) present.
        if (packet.Payload.Length >= 49)
        {
            for (var i = 0; i < 4; i++)
            {
                VoltagesExtMv[i] = BitConverter.ToUInt16(FullPacket, 41 + i * 2);
            }
        }
    }

    public override void Print()
    {
        Console.WriteLine(
            $"BATTERY_STATUS: Id={Id}, Remaining={BatteryRemainingPct}%, Cells=[{string.Join(",", VoltagesMv)}]");
    }
}
