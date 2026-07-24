namespace tLogViewer.Core.Models.Messages;

/// <summary>MAVLink EXTENDED_SYS_STATE (245).</summary>
public sealed class ExtendedSysState : MavLinkMessage
{
    /// <summary>MAV_VTOL_STATE.</summary>
    public byte VtolState;
    /// <summary>MAV_LANDED_STATE.</summary>
    public byte LandedState;

    public override int ExpectedLength => 2;

    public ExtendedSysState(MavPacket packet) : base(packet)
    {
        VtolState = FullPacket[0];
        LandedState = FullPacket[1];
    }

    public override void Print() =>
        Console.WriteLine($"EXTENDED_SYS_STATE: VtolState={VtolState}, LandedState={LandedState}");
}
