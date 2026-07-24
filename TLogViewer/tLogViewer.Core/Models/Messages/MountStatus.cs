namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink MOUNT_STATUS (158).</summary>
public sealed class MountStatus : MavLinkMessage
{
    /// <summary>cdeg; pitch.</summary>
    public int PointingA;
    /// <summary>cdeg; roll.</summary>
    public int PointingB;
    /// <summary>cdeg; yaw.</summary>
    public int PointingC;
    public byte TargetSystem;
    public byte TargetComponent;

    /// <summary>Base 14; mount_mode extension ignored.</summary>
    public override int ExpectedLength => 14;

    public MountStatus(MavPacket packet) : base(packet)
    {
        PointingA = BitConverter.ToInt32(FullPacket, 0);
        PointingB = BitConverter.ToInt32(FullPacket, 4);
        PointingC = BitConverter.ToInt32(FullPacket, 8);
        TargetSystem = FullPacket[12];
        TargetComponent = FullPacket[13];
    }

    public override void Print() =>
        Console.WriteLine($"MOUNT_STATUS: Pitch={PointingA / 100.0:F1}, Roll={PointingB / 100.0:F1}, Yaw={PointingC / 100.0:F1}");
}
