namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink DISTANCE_SENSOR (132).</summary>
public sealed class DistanceSensor : MavLinkMessage
{
    public uint TimeBootMs;
    public ushort MinDistanceCm;
    public ushort MaxDistanceCm;
    public ushort CurrentDistanceCm;
    /// <summary>MAV_DISTANCE_SENSOR.</summary>
    public byte Type;
    public byte Id;
    /// <summary>MAV_SENSOR_ORIENTATION.</summary>
    public byte Orientation;
    public byte Covariance;

    /// <summary>Base 14; quaternion/horizontal_fov/vertical_fov/signal_quality extensions ignored.</summary>
    public override int ExpectedLength => 14;

    public DistanceSensor(MavPacket packet) : base(packet)
    {
        TimeBootMs = BitConverter.ToUInt32(FullPacket, 0);
        MinDistanceCm = BitConverter.ToUInt16(FullPacket, 4);
        MaxDistanceCm = BitConverter.ToUInt16(FullPacket, 6);
        CurrentDistanceCm = BitConverter.ToUInt16(FullPacket, 8);
        Type = FullPacket[10];
        Id = FullPacket[11];
        Orientation = FullPacket[12];
        Covariance = FullPacket[13];
    }

    public override void Print() =>
        Console.WriteLine($"DISTANCE_SENSOR: Id={Id}, Distance={CurrentDistanceCm}cm, Range=[{MinDistanceCm},{MaxDistanceCm}]cm");
}
