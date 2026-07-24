namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink GENERATOR_STATUS (373).</summary>
public sealed class GeneratorStatus : MavLinkMessage
{
    /// <summary>Bitmap of MAV_GENERATOR_STATUS_FLAG.</summary>
    public ulong Status;
    /// <summary>A; positive for out, negative for in.</summary>
    public float BatteryCurrentA;
    /// <summary>A; positive for out, negative for in.</summary>
    public float LoadCurrentA;
    /// <summary>W.</summary>
    public float PowerGeneratedW;
    /// <summary>V.</summary>
    public float BusVoltageV;
    /// <summary>A; target battery current.</summary>
    public float BatCurrentSetpointA;
    /// <summary>Seconds since reboot; UINT32_MAX if not provided.</summary>
    public uint RuntimeS;
    /// <summary>Seconds until maintenance required; negative if past-due.</summary>
    public int TimeUntilMaintenanceS;
    /// <summary>rpm; UINT16_MAX if not provided.</summary>
    public ushort GeneratorSpeedRpm;
    /// <summary>degC; INT16_MAX if not provided.</summary>
    public short RectifierTemperatureCdegC;
    /// <summary>degC; INT16_MAX if not provided.</summary>
    public short GeneratorTemperatureCdegC;

    public override int ExpectedLength => 42;

    public GeneratorStatus(MavPacket packet) : base(packet)
    {
        Status = BitConverter.ToUInt64(FullPacket, 0);
        BatteryCurrentA = BitConverter.ToSingle(FullPacket, 8);
        LoadCurrentA = BitConverter.ToSingle(FullPacket, 12);
        PowerGeneratedW = BitConverter.ToSingle(FullPacket, 16);
        BusVoltageV = BitConverter.ToSingle(FullPacket, 20);
        BatCurrentSetpointA = BitConverter.ToSingle(FullPacket, 24);
        RuntimeS = BitConverter.ToUInt32(FullPacket, 28);
        TimeUntilMaintenanceS = BitConverter.ToInt32(FullPacket, 32);
        GeneratorSpeedRpm = BitConverter.ToUInt16(FullPacket, 36);
        RectifierTemperatureCdegC = BitConverter.ToInt16(FullPacket, 38);
        GeneratorTemperatureCdegC = BitConverter.ToInt16(FullPacket, 40);
    }

    public override void Print() =>
        Console.WriteLine($"GENERATOR_STATUS: Status={Status}, Speed={GeneratorSpeedRpm}rpm, BusVoltage={BusVoltageV:F1}V");
}
