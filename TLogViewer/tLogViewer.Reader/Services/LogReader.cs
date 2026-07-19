using System.Buffers.Binary;
using tLogViewer.Core.Models;

namespace tLogViewer.Reader.Services
{
    public class LogReader
    {
        private const byte Mavlink1Stx = 0xFE;
        private const byte Mavlink2Stx = 0xFD;
        private const byte MavlinkIflagSigned = 0x01;
        private const int Mavlink2SignatureLength = 13;

        public static IEnumerable<TLogRecord> ReadTLog(string file)
        {
            using var fs = File.OpenRead(file);
            foreach (var record in ReadTLog(fs))
            {
                yield return record;
            }
        }

        public static IEnumerable<TLogRecord> ReadTLog(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new ArgumentException("TLog stream must be seekable.", nameof(stream));
            }

            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

            if (stream.Length < 9)
                yield break;

            ulong trail = ReadTrail(reader);
            byte stx = reader.ReadByte();

            if (!IsValidStx(stx))
                throw new InvalidDataException($"Unknown TLOG format. First packet starts with 0x{stx:X2}");

            while (true)
            {
                if (!IsValidStx(stx) && !TryResync(reader, ref trail, ref stx))
                    yield break;

                byte len = reader.ReadByte();
                int remaining = GetRemainingHeaderPayloadCrcLength(reader, stx, len);

                if (remaining < 0 || reader.BaseStream.Position + remaining > reader.BaseStream.Length)
                    yield break;

                byte[] packet = new byte[2 + remaining];
                packet[0] = stx;
                packet[1] = len;

                int read = reader.Read(packet, 2, remaining);
                if (read != remaining)
                    yield break;

                // Drop MAVLink2 signature from packet bytes used for parsing.
                if (stx == Mavlink2Stx && remaining >= 8 + len + 2 + Mavlink2SignatureLength)
                {
                    byte incompat = packet[2];
                    if ((incompat & MavlinkIflagSigned) != 0)
                    {
                        var unsignedLength = 2 + 8 + len + 2;
                        var unsignedPacket = new byte[unsignedLength];
                        Array.Copy(packet, 0, unsignedPacket, 0, unsignedLength);
                        packet = unsignedPacket;
                    }
                }

                yield return new TLogRecord
                {
                    Trail = trail,
                    Packet = packet,
                    MavPacket = MavPacket.FromBytes(packet)
                };

                if (reader.BaseStream.Position >= reader.BaseStream.Length)
                    yield break;

                trail = ReadTrail(reader);

                if (reader.BaseStream.Position >= reader.BaseStream.Length)
                    yield break;

                stx = reader.ReadByte();
            }
        }

        private static bool IsValidStx(byte stx) => stx is Mavlink1Stx or Mavlink2Stx;

        /// <summary>
        /// TLog timestamps are big-endian uint64 microseconds since Unix epoch.
        /// </summary>
        private static ulong ReadTrail(BinaryReader reader) =>
            BinaryPrimitives.ReverseEndianness(reader.ReadUInt64());

        private static int GetRemainingHeaderPayloadCrcLength(BinaryReader reader, byte stx, byte payloadLen)
        {
            if (stx == Mavlink1Stx)
            {
                // seq sys comp msg payload crc
                return 4 + payloadLen + 2;
            }

            // Peek incompat flags to account for optional signature.
            if (reader.BaseStream.Position >= reader.BaseStream.Length)
                return -1;

            byte incompat = reader.ReadByte();
            reader.BaseStream.Position -= 1;

            var signature = (incompat & MavlinkIflagSigned) != 0 ? Mavlink2SignatureLength : 0;
            // incompat compat seq sys comp msg(3) payload crc [signature]
            return 8 + payloadLen + 2 + signature;
        }

        /// <summary>
        /// After a bad magic byte, scan forward for the next MAVLink STX and
        /// treat the preceding 8 bytes as the TLog timestamp trail.
        /// </summary>
        private static bool TryResync(BinaryReader reader, ref ulong trail, ref byte stx)
        {
            var stream = reader.BaseStream;

            while (stream.Position < stream.Length)
            {
                byte candidate = reader.ReadByte();
                if (!IsValidStx(candidate))
                    continue;

                if (stream.Position < 9)
                    continue;

                var magicPos = stream.Position - 1;
                stream.Position = magicPos - 8;
                trail = ReadTrail(reader);
                stx = reader.ReadByte();
                return IsValidStx(stx);
            }

            return false;
        }
    }
}
