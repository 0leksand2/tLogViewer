namespace tLogViewer.DTO.Messages;

public sealed class NavControllerOutputData
{
    public float NavRoll { get; init; }
    public float NavPitch { get; init; }
    public short NavBearing { get; init; }
    public short TargetBearing { get; init; }
    public ushort WpDistM { get; init; }
    public float AltErrorM { get; init; }
    public float AspdErrorMS { get; init; }
    public float XtrackErrorM { get; init; }
}
