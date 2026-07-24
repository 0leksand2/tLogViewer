namespace tLogViewer.Core.Models.Messages;

/// <summary>ArduPilot PID_TUNING (194).</summary>
public sealed class PidTuning : MavLinkMessage
{
    public float Desired;
    public float Achieved;
    public float FF;
    public float P;
    public float I;
    public float D;
    /// <summary>PID_TUNING_AXIS.</summary>
    public byte Axis;

    /// <summary>Base 25; SRate/PDmod extensions ignored.</summary>
    public override int ExpectedLength => 25;

    public PidTuning(MavPacket packet) : base(packet)
    {
        Desired = BitConverter.ToSingle(FullPacket, 0);
        Achieved = BitConverter.ToSingle(FullPacket, 4);
        FF = BitConverter.ToSingle(FullPacket, 8);
        P = BitConverter.ToSingle(FullPacket, 12);
        I = BitConverter.ToSingle(FullPacket, 16);
        D = BitConverter.ToSingle(FullPacket, 20);
        Axis = FullPacket[24];
    }

    public override void Print() =>
        Console.WriteLine($"PID_TUNING: Axis={Axis}, Desired={Desired:F2}, Achieved={Achieved:F2}, P={P:F3}, I={I:F3}, D={D:F3}");
}
