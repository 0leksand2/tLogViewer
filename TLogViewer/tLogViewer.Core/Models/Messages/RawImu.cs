namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink RAW_IMU (27).</summary>
public sealed class RawImu : MavLinkMessage
{
    public ulong TimeUsec;
    public short XAcc;
    public short YAcc;
    public short ZAcc;
    public short XGyro;
    public short YGyro;
    public short ZGyro;
    public short XMag;
    public short YMag;
    public short ZMag;
    public byte Id;
    /// <summary>cdegC; null when extension absent or INT16_MAX.</summary>
    public short? TemperatureCdegC;

    /// <summary>Base 26; id + temperature → 29.</summary>
    public override int ExpectedLength => 29;

    public RawImu(MavPacket packet) : base(packet)
    {
        TimeUsec = BitConverter.ToUInt64(FullPacket, 0);
        XAcc = BitConverter.ToInt16(FullPacket, 8);
        YAcc = BitConverter.ToInt16(FullPacket, 10);
        ZAcc = BitConverter.ToInt16(FullPacket, 12);
        XGyro = BitConverter.ToInt16(FullPacket, 14);
        YGyro = BitConverter.ToInt16(FullPacket, 16);
        ZGyro = BitConverter.ToInt16(FullPacket, 18);
        XMag = BitConverter.ToInt16(FullPacket, 20);
        YMag = BitConverter.ToInt16(FullPacket, 22);
        ZMag = BitConverter.ToInt16(FullPacket, 24);
        if (packet.Payload.Length >= 27)
        {
            Id = FullPacket[26];
        }

        if (packet.Payload.Length >= 29)
        {
            var temp = BitConverter.ToInt16(FullPacket, 27);
            TemperatureCdegC = temp == short.MaxValue ? null : temp;
        }
    }

    public override void Print() =>
        Console.WriteLine($"RAW_IMU: Id={Id} Acc=({XAcc},{YAcc},{ZAcc})");
}
