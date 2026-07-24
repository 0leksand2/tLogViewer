namespace tLogViewer.Core.Models.Messages;

/// <summary>ArduPilot AHRS2 (178) — status of secondary AHRS filter.</summary>
public sealed class Ahrs2 : MavLinkMessage
{
    /// <summary>rad.</summary>
    public float Roll;
    /// <summary>rad.</summary>
    public float Pitch;
    /// <summary>rad.</summary>
    public float Yaw;
    /// <summary>Altitude MSL (m).</summary>
    public float Altitude;
    public int Latitude;
    public int Longitude;

    public double RollDeg;
    public double PitchDeg;
    public double YawDeg;
    public double LatitudeDeg;
    public double LongitudeDeg;

    public override int ExpectedLength => 24;

    public Ahrs2(MavPacket packet) : base(packet)
    {
        Roll = BitConverter.ToSingle(FullPacket, 0);
        Pitch = BitConverter.ToSingle(FullPacket, 4);
        Yaw = BitConverter.ToSingle(FullPacket, 8);
        Altitude = BitConverter.ToSingle(FullPacket, 12);
        Latitude = BitConverter.ToInt32(FullPacket, 16);
        Longitude = BitConverter.ToInt32(FullPacket, 20);

        RollDeg = Roll * (180.0 / Math.PI);
        PitchDeg = Pitch * (180.0 / Math.PI);
        YawDeg = Yaw * (180.0 / Math.PI);
        LatitudeDeg = Latitude / 1e7;
        LongitudeDeg = Longitude / 1e7;
    }

    public override void Print() =>
        Console.WriteLine($"AHRS2: Roll={RollDeg:F1}, Pitch={PitchDeg:F1}, Yaw={YawDeg:F1}, Lat={LatitudeDeg:F7}, Lon={LongitudeDeg:F7}");
}
