namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink AUTOPILOT_VERSION (148).</summary>
public sealed class AutopilotVersion : MavLinkMessage
{
    /// <summary>Bitmap of MAV_PROTOCOL_CAPABILITY.</summary>
    public ulong Capabilities;
    /// <summary>UID if provided by hardware (see Uid2 extension).</summary>
    public ulong Uid;
    public uint FlightSwVersion;
    public uint MiddlewareSwVersion;
    public uint OsSwVersion;
    public uint BoardVersion;
    public ushort VendorId;
    public ushort ProductId;
    public byte[] FlightCustomVersion = new byte[8];
    public byte[] MiddlewareCustomVersion = new byte[8];
    public byte[] OsCustomVersion = new byte[8];

    /// <summary>Base 60; uid2 extension ignored.</summary>
    public override int ExpectedLength => 60;

    public AutopilotVersion(MavPacket packet) : base(packet)
    {
        Capabilities = BitConverter.ToUInt64(FullPacket, 0);
        Uid = BitConverter.ToUInt64(FullPacket, 8);
        FlightSwVersion = BitConverter.ToUInt32(FullPacket, 16);
        MiddlewareSwVersion = BitConverter.ToUInt32(FullPacket, 20);
        OsSwVersion = BitConverter.ToUInt32(FullPacket, 24);
        BoardVersion = BitConverter.ToUInt32(FullPacket, 28);
        VendorId = BitConverter.ToUInt16(FullPacket, 32);
        ProductId = BitConverter.ToUInt16(FullPacket, 34);
        Buffer.BlockCopy(FullPacket, 36, FlightCustomVersion, 0, 8);
        Buffer.BlockCopy(FullPacket, 44, MiddlewareCustomVersion, 0, 8);
        Buffer.BlockCopy(FullPacket, 52, OsCustomVersion, 0, 8);
    }

    public override void Print() =>
        Console.WriteLine($"AUTOPILOT_VERSION: FlightSw={FlightSwVersion}, Board={BoardVersion}, Vendor={VendorId}, Product={ProductId}");
}
