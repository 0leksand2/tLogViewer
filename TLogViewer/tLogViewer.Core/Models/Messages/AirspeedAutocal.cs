namespace tLogViewer.Core.Models.Messages;

/// <summary>ArduPilot AIRSPEED_AUTOCAL (174).</summary>
public sealed class AirspeedAutocal : MavLinkMessage
{
    /// <summary>GPS velocity north (m/s).</summary>
    public float Vx;
    /// <summary>GPS velocity east (m/s).</summary>
    public float Vy;
    /// <summary>GPS velocity down (m/s).</summary>
    public float Vz;
    /// <summary>Differential pressure (Pa).</summary>
    public float DiffPressure;
    public float Eas2Tas;
    public float Ratio;
    /// <summary>EKF state x.</summary>
    public float StateX;
    /// <summary>EKF state y.</summary>
    public float StateY;
    /// <summary>EKF state z.</summary>
    public float StateZ;
    public float Pax;
    public float Pby;
    public float Pcz;

    public override int ExpectedLength => 48;

    public AirspeedAutocal(MavPacket packet) : base(packet)
    {
        Vx = BitConverter.ToSingle(FullPacket, 0);
        Vy = BitConverter.ToSingle(FullPacket, 4);
        Vz = BitConverter.ToSingle(FullPacket, 8);
        DiffPressure = BitConverter.ToSingle(FullPacket, 12);
        Eas2Tas = BitConverter.ToSingle(FullPacket, 16);
        Ratio = BitConverter.ToSingle(FullPacket, 20);
        StateX = BitConverter.ToSingle(FullPacket, 24);
        StateY = BitConverter.ToSingle(FullPacket, 28);
        StateZ = BitConverter.ToSingle(FullPacket, 32);
        Pax = BitConverter.ToSingle(FullPacket, 36);
        Pby = BitConverter.ToSingle(FullPacket, 40);
        Pcz = BitConverter.ToSingle(FullPacket, 44);
    }

    public override void Print() =>
        Console.WriteLine($"AIRSPEED_AUTOCAL: Ratio={Ratio:F3}, EAS2TAS={Eas2Tas:F3}");
}
