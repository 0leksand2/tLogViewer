namespace tLogViewer.DTO.Messages;

public sealed class LocalPositionNedData
{
    public uint TimeBootMs { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
    public float Vx { get; init; }
    public float Vy { get; init; }
    public float Vz { get; init; }
}
