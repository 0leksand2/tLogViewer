namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink OPTICAL_FLOW (100).</summary>
public sealed class OpticalFlow : MavLinkMessage
{
    public ulong TimeUsec;
    public float FlowCompMX;
    public float FlowCompMY;
    public float GroundDistanceM;
    public short FlowX;
    public short FlowY;
    public byte SensorId;
    public byte Quality;
    /// <summary>rad/s; extension, null when payload shorter than 34 bytes.</summary>
    public float? FlowRateX;
    /// <summary>rad/s; extension, null when payload shorter than 34 bytes.</summary>
    public float? FlowRateY;

    /// <summary>Base 26; flow_rate_x/flow_rate_y extension → 34.</summary>
    public override int ExpectedLength => 34;

    public OpticalFlow(MavPacket packet) : base(packet)
    {
        TimeUsec = BitConverter.ToUInt64(FullPacket, 0);
        FlowCompMX = BitConverter.ToSingle(FullPacket, 8);
        FlowCompMY = BitConverter.ToSingle(FullPacket, 12);
        GroundDistanceM = BitConverter.ToSingle(FullPacket, 16);
        FlowX = BitConverter.ToInt16(FullPacket, 20);
        FlowY = BitConverter.ToInt16(FullPacket, 22);
        SensorId = FullPacket[24];
        Quality = FullPacket[25];

        if (packet.Payload.Length >= 34)
        {
            FlowRateX = BitConverter.ToSingle(FullPacket, 26);
            FlowRateY = BitConverter.ToSingle(FullPacket, 30);
        }
    }

    public override void Print() =>
        Console.WriteLine($"OPTICAL_FLOW: Id={SensorId}, Flow=({FlowX},{FlowY}), Quality={Quality}");
}
