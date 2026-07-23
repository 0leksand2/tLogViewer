namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink VIBRATION (241).</summary>
public sealed class Vibration : MavLinkMessage
{
    public ulong TimeUsec;
    public float VibrationX;
    public float VibrationY;
    public float VibrationZ;
    public uint Clipping0;
    public uint Clipping1;
    public uint Clipping2;

    public override int ExpectedLength => 32;

    public Vibration(MavPacket packet) : base(packet)
    {
        TimeUsec = BitConverter.ToUInt64(FullPacket, 0);
        VibrationX = BitConverter.ToSingle(FullPacket, 8);
        VibrationY = BitConverter.ToSingle(FullPacket, 12);
        VibrationZ = BitConverter.ToSingle(FullPacket, 16);
        Clipping0 = BitConverter.ToUInt32(FullPacket, 20);
        Clipping1 = BitConverter.ToUInt32(FullPacket, 24);
        Clipping2 = BitConverter.ToUInt32(FullPacket, 28);
    }

    public override void Print() =>
        Console.WriteLine($"VIBRATION: ({VibrationX},{VibrationY},{VibrationZ})");
}
