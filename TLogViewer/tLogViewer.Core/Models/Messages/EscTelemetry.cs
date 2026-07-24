namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink ESC_TELEMETRY_1_TO_4 (11030) style — also covers _5_TO_8/_9_TO_12/_13_TO_16 (identical layout, different ESC range).</summary>
public sealed class EscTelemetry : MavLinkMessage
{
    /// <summary>cV.</summary>
    public ushort[] VoltageCv = new ushort[4];
    /// <summary>cA.</summary>
    public ushort[] CurrentCa = new ushort[4];
    /// <summary>mAh.</summary>
    public ushort[] TotalCurrentMah = new ushort[4];
    /// <summary>eRPM.</summary>
    public ushort[] RpmErpm = new ushort[4];
    /// <summary>Count of telemetry packets received (wraps at 65535).</summary>
    public ushort[] Count = new ushort[4];
    /// <summary>degC.</summary>
    public byte[] TemperatureDegC = new byte[4];

    /// <summary>Index of the first ESC covered by this message: 1, 5, 9 or 13. Set by the caller/factory based on msg id.</summary>
    public int FirstEscIndex { get; set; }

    public override int ExpectedLength => 44;

    public EscTelemetry(MavPacket packet) : base(packet)
    {
        for (var i = 0; i < 4; i++)
        {
            VoltageCv[i] = BitConverter.ToUInt16(FullPacket, i * 2);
            CurrentCa[i] = BitConverter.ToUInt16(FullPacket, 8 + i * 2);
            TotalCurrentMah[i] = BitConverter.ToUInt16(FullPacket, 16 + i * 2);
            RpmErpm[i] = BitConverter.ToUInt16(FullPacket, 24 + i * 2);
            Count[i] = BitConverter.ToUInt16(FullPacket, 32 + i * 2);
        }

        Buffer.BlockCopy(FullPacket, 40, TemperatureDegC, 0, 4);
    }

    public override void Print() =>
        Console.WriteLine($"ESC_TELEMETRY: FirstEsc={FirstEscIndex}, Rpm=[{string.Join(",", RpmErpm)}]");
}
