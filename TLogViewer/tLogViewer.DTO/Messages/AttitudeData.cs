namespace tLogViewer.DTO.Messages;

public sealed class AttitudeData
{
    public uint TimeBootMs { get; init; }
    public double RollDeg { get; init; }
    public double PitchDeg { get; init; }
    public double YawDeg { get; init; }
    public float RollSpeed { get; init; }
    public float PitchSpeed { get; init; }
    public float YawSpeed { get; init; }
}
