namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink SCALED_PRESSURE (29). Same layout also used for SCALED_PRESSURE2 (137).</summary>
public sealed class ScaledPressure : MavLinkMessage
{
    public uint TimeBootMs;
    public float PressAbsHpa;
    public float PressDiffHpa;
    public short TemperatureCdegC;
    /// <summary>cdegC; extension, null when payload shorter than 16 bytes.</summary>
    public short? TemperaturePressDiffCdegC;

    /// <summary>Base 14; temperature_press_diff extension → 16.</summary>
    public override int ExpectedLength => 16;

    public ScaledPressure(MavPacket packet) : base(packet)
    {
        TimeBootMs = BitConverter.ToUInt32(FullPacket, 0);
        PressAbsHpa = BitConverter.ToSingle(FullPacket, 4);
        PressDiffHpa = BitConverter.ToSingle(FullPacket, 8);
        TemperatureCdegC = BitConverter.ToInt16(FullPacket, 12);

        if (packet.Payload.Length >= 16)
        {
            TemperaturePressDiffCdegC = BitConverter.ToInt16(FullPacket, 14);
        }
    }

    public override void Print() =>
        Console.WriteLine($"SCALED_PRESSURE: Abs={PressAbsHpa:F2}hPa, Diff={PressDiffHpa:F2}hPa, Temp={TemperatureCdegC / 100.0:F1}C");
}
