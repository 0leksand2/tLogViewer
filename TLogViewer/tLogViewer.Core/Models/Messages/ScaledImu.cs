namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink SCALED_IMU (26) / SCALED_IMU2 (116) / SCALED_IMU3 (129).</summary>
public sealed class ScaledImu : MavLinkMessage
{
    public uint TimeBootMs;
    public short XAccMg;
    public short YAccMg;
    public short ZAccMg;
    public short XGyroMradS;
    public short YGyroMradS;
    public short ZGyroMradS;
    public short XMagMgauss;
    public short YMagMgauss;
    public short ZMagMgauss;
    /// <summary>cdegC; null when extension absent or INT16_MAX.</summary>
    public short? TemperatureCdegC;

    /// <summary>Base 22; temperature extension → 24.</summary>
    public override int ExpectedLength => 24;

    public ScaledImu(MavPacket packet) : base(packet)
    {
        TimeBootMs = BitConverter.ToUInt32(FullPacket, 0);
        XAccMg = BitConverter.ToInt16(FullPacket, 4);
        YAccMg = BitConverter.ToInt16(FullPacket, 6);
        ZAccMg = BitConverter.ToInt16(FullPacket, 8);
        XGyroMradS = BitConverter.ToInt16(FullPacket, 10);
        YGyroMradS = BitConverter.ToInt16(FullPacket, 12);
        ZGyroMradS = BitConverter.ToInt16(FullPacket, 14);
        XMagMgauss = BitConverter.ToInt16(FullPacket, 16);
        YMagMgauss = BitConverter.ToInt16(FullPacket, 18);
        ZMagMgauss = BitConverter.ToInt16(FullPacket, 20);
        if (packet.Payload.Length >= 24)
        {
            var temp = BitConverter.ToInt16(FullPacket, 22);
            TemperatureCdegC = temp == short.MaxValue ? null : temp;
        }
    }

    public override void Print() =>
        Console.WriteLine($"SCALED_IMU: Acc=({XAccMg},{YAccMg},{ZAccMg}) mG");
}
