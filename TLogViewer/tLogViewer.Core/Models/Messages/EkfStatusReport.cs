namespace tLogViewer.Core.Models.Messages;

/// <summary>ArduPilot EKF_STATUS_REPORT (193).</summary>
public sealed class EkfStatusReport : MavLinkMessage
{
    public float VelocityVariance;
    public float PosHorizVariance;
    public float PosVertVariance;
    public float CompassVariance;
    public float TerrainAltVariance;
    public ushort Flags;
    public float AirspeedVariance;

    /// <summary>Base 22; airspeed_variance → 26.</summary>
    public override int ExpectedLength => 26;

    public EkfStatusReport(MavPacket packet) : base(packet)
    {
        VelocityVariance = BitConverter.ToSingle(FullPacket, 0);
        PosHorizVariance = BitConverter.ToSingle(FullPacket, 4);
        PosVertVariance = BitConverter.ToSingle(FullPacket, 8);
        CompassVariance = BitConverter.ToSingle(FullPacket, 12);
        TerrainAltVariance = BitConverter.ToSingle(FullPacket, 16);
        Flags = BitConverter.ToUInt16(FullPacket, 20);
        if (packet.Payload.Length >= 26)
        {
            AirspeedVariance = BitConverter.ToSingle(FullPacket, 22);
        }
    }

    public override void Print() =>
        Console.WriteLine($"EKF_STATUS_REPORT: Flags={Flags}, VelVar={VelocityVariance}");
}
