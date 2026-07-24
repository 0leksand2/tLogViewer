namespace tLogViewer.Core.Models.Messages;

/// <summary>uAvionix UAVIONIX_ADSB_OUT_STATUS (10008). Simplified subset (excludes flight_id char[8]).</summary>
public sealed class UavionixAdsbOutStatus : MavLinkMessage
{
    /// <summary>Mode A squawk code (e.g. 1200 for VFR).</summary>
    public ushort Squawk;
    /// <summary>UAVIONIX_ADSB_OUT_STATUS_STATE bitmap.</summary>
    public byte State;
    /// <summary>NACp (bits 7:4) / NIC (bits 3:0).</summary>
    public byte NicNacp;
    /// <summary>Board temperature in C.</summary>
    public byte BoardTemp;
    /// <summary>UAVIONIX_ADSB_OUT_STATUS_FAULT bitmap.</summary>
    public byte Fault;

    /// <summary>squawk (2) + state/NIC_NACp/board_temp/fault (4x1) = 6 bytes; flight_id (8 bytes) omitted for simplicity.</summary>
    public override int ExpectedLength => 6;

    public UavionixAdsbOutStatus(MavPacket packet) : base(packet)
    {
        Squawk = BitConverter.ToUInt16(FullPacket, 0);
        State = FullPacket[2];
        NicNacp = FullPacket[3];
        BoardTemp = FullPacket[4];
        Fault = FullPacket[5];
    }

    public override void Print() =>
        Console.WriteLine($"UAVIONIX_ADSB_OUT_STATUS: Squawk={Squawk}, State={State}, Fault={Fault}");
}
