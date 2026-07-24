namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink HYGROMETER_SENSOR (12920).</summary>
public sealed class HygrometerSensor : MavLinkMessage
{
    /// <summary>cdegC.</summary>
    public short TemperatureCdegC;
    /// <summary>c%.</summary>
    public ushort HumidityCpct;
    /// <summary>Hygrometer instance.</summary>
    public byte Id;

    public override int ExpectedLength => 5;

    public HygrometerSensor(MavPacket packet) : base(packet)
    {
        TemperatureCdegC = BitConverter.ToInt16(FullPacket, 0);
        HumidityCpct = BitConverter.ToUInt16(FullPacket, 2);
        Id = FullPacket[4];
    }

    public override void Print() =>
        Console.WriteLine($"HYGROMETER_SENSOR: Id={Id}, Temp={TemperatureCdegC / 100.0:F1}C, Humidity={HumidityCpct / 100.0:F1}%");
}
