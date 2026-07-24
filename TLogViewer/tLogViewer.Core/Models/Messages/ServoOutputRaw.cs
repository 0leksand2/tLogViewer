namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink SERVO_OUTPUT_RAW (36).</summary>
public sealed class ServoOutputRaw : MavLinkMessage
{
    public uint TimeUsec;
    public byte Port;
    /// <summary>Servo1Raw..Servo16Raw (index 0..15). Index 8..15 (servo9..16) are the mavlink2 extension.</summary>
    public ushort[] ServoRaw = new ushort[16];

    /// <summary>Base 21 (servo1..8); servo9..16 extension → 37.</summary>
    public override int ExpectedLength => 37;

    public ServoOutputRaw(MavPacket packet) : base(packet)
    {
        TimeUsec = BitConverter.ToUInt32(FullPacket, 0);

        for (var i = 0; i < 8; i++)
        {
            ServoRaw[i] = BitConverter.ToUInt16(FullPacket, 4 + i * 2);
        }

        Port = FullPacket[20];

        if (packet.Payload.Length >= 37)
        {
            for (var i = 8; i < 16; i++)
            {
                ServoRaw[i] = BitConverter.ToUInt16(FullPacket, 21 + (i - 8) * 2);
            }
        }
    }

    public override void Print() =>
        Console.WriteLine($"SERVO_OUTPUT_RAW: Port={Port}, Servo1-4=[{string.Join(",", ServoRaw[..4])}]");
}
