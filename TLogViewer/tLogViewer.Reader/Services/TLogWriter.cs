using System.Buffers.Binary;

namespace tLogViewer.Reader.Services;

public static class TLogWriter
{
    /// <summary>
    /// Writes one TLog entry: 8-byte big-endian microsecond trail + raw MAVLink packet bytes.
    /// </summary>
    public static void WriteRecord(Stream stream, ulong trailUs, ReadOnlySpan<byte> packet)
    {
        Span<byte> trailBytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(trailBytes, trailUs);
        stream.Write(trailBytes);
        stream.Write(packet);
    }
}
