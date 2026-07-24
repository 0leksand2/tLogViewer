namespace tLogViewer.Core.Models.Messages;

/// <summary>ArduPilot EFI_STATUS (225).</summary>
public sealed class EfiStatus : MavLinkMessage
{
    public float EcuIndex;
    /// <summary>RPM.</summary>
    public float Rpm;
    /// <summary>cm^3.</summary>
    public float FuelConsumed;
    /// <summary>cm^3/min.</summary>
    public float FuelFlow;
    /// <summary>%.</summary>
    public float EngineLoad;
    /// <summary>%.</summary>
    public float ThrottlePosition;
    /// <summary>ms.</summary>
    public float SparkDwellTime;
    /// <summary>kPa.</summary>
    public float BarometricPressure;
    /// <summary>kPa.</summary>
    public float IntakeManifoldPressure;
    /// <summary>degC.</summary>
    public float IntakeManifoldTemperature;
    /// <summary>degC.</summary>
    public float CylinderHeadTemperature;
    /// <summary>deg; crank angle.</summary>
    public float IgnitionTiming;
    /// <summary>ms.</summary>
    public float InjectionTime;
    /// <summary>degC.</summary>
    public float ExhaustGasTemperature;
    /// <summary>%.</summary>
    public float ThrottleOut;
    public float PtCompensation;
    /// <summary>EFI health status.</summary>
    public byte Health;

    /// <summary>Base 65; ignition_voltage/fuel_pressure extensions ignored.</summary>
    public override int ExpectedLength => 65;

    public EfiStatus(MavPacket packet) : base(packet)
    {
        EcuIndex = BitConverter.ToSingle(FullPacket, 0);
        Rpm = BitConverter.ToSingle(FullPacket, 4);
        FuelConsumed = BitConverter.ToSingle(FullPacket, 8);
        FuelFlow = BitConverter.ToSingle(FullPacket, 12);
        EngineLoad = BitConverter.ToSingle(FullPacket, 16);
        ThrottlePosition = BitConverter.ToSingle(FullPacket, 20);
        SparkDwellTime = BitConverter.ToSingle(FullPacket, 24);
        BarometricPressure = BitConverter.ToSingle(FullPacket, 28);
        IntakeManifoldPressure = BitConverter.ToSingle(FullPacket, 32);
        IntakeManifoldTemperature = BitConverter.ToSingle(FullPacket, 36);
        CylinderHeadTemperature = BitConverter.ToSingle(FullPacket, 40);
        IgnitionTiming = BitConverter.ToSingle(FullPacket, 44);
        InjectionTime = BitConverter.ToSingle(FullPacket, 48);
        ExhaustGasTemperature = BitConverter.ToSingle(FullPacket, 52);
        ThrottleOut = BitConverter.ToSingle(FullPacket, 56);
        PtCompensation = BitConverter.ToSingle(FullPacket, 60);
        Health = FullPacket[64];
    }

    public override void Print() =>
        Console.WriteLine($"EFI_STATUS: Health={Health}, Rpm={Rpm:F0}, EGT={ExhaustGasTemperature:F1}C");
}
