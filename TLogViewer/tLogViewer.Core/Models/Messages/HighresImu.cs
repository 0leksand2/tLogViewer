namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink HIGHRES_IMU (105) — SI units.</summary>
public sealed class HighresImu : MavLinkMessage
{
    public ulong TimeUsec;
    public float XAccMs2;
    public float YAccMs2;
    public float ZAccMs2;
    public float XGyroRadS;
    public float YGyroRadS;
    public float ZGyroRadS;
    public float XMagGauss;
    public float YMagGauss;
    public float ZMagGauss;
    public float AbsPressureHpa;
    public float DiffPressureHpa;
    public float PressureAlt;
    public float TemperatureC;
    public ushort FieldsUpdated;
    public byte Id;

    /// <summary>Base 62; id extension → 63.</summary>
    public override int ExpectedLength => 63;

    public HighresImu(MavPacket packet) : base(packet)
    {
        TimeUsec = BitConverter.ToUInt64(FullPacket, 0);
        XAccMs2 = BitConverter.ToSingle(FullPacket, 8);
        YAccMs2 = BitConverter.ToSingle(FullPacket, 12);
        ZAccMs2 = BitConverter.ToSingle(FullPacket, 16);
        XGyroRadS = BitConverter.ToSingle(FullPacket, 20);
        YGyroRadS = BitConverter.ToSingle(FullPacket, 24);
        ZGyroRadS = BitConverter.ToSingle(FullPacket, 28);
        XMagGauss = BitConverter.ToSingle(FullPacket, 32);
        YMagGauss = BitConverter.ToSingle(FullPacket, 36);
        ZMagGauss = BitConverter.ToSingle(FullPacket, 40);
        AbsPressureHpa = BitConverter.ToSingle(FullPacket, 44);
        DiffPressureHpa = BitConverter.ToSingle(FullPacket, 48);
        PressureAlt = BitConverter.ToSingle(FullPacket, 52);
        TemperatureC = BitConverter.ToSingle(FullPacket, 56);
        FieldsUpdated = BitConverter.ToUInt16(FullPacket, 60);
        if (packet.Payload.Length >= 63)
        {
            Id = FullPacket[62];
        }
    }

    public override void Print() =>
        Console.WriteLine($"HIGHRES_IMU: Id={Id} Acc=({XAccMs2},{YAccMs2},{ZAccMs2})");
}
