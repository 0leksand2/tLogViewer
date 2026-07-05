using Asv.IO;
using tLogViewer.Reader.Models;

namespace tLogViewer.Reader.Services
{
    public class LogReader
    {
        public static IEnumerable<TLogRecord> ReadTLog(string file)
        {
            using var fs = File.OpenRead(file);
            using var reader = new BinaryReader(fs);

            // Перевіряємо перший запис
            if (fs.Length < 9)
                yield break;

            ulong trail = reader.ReadUInt64().Reverse();

            byte stx = reader.ReadByte();

            if (stx != 0xFE && stx != 0xFD)
                throw new InvalidDataException($"Unknown TLOG format. First packet starts with 0x{stx:X2}");

            while (true)
            {
                byte len = reader.ReadByte();

                int remaining;

                if (stx == 0xFE)
                {
                    // seq sys comp msg payload crc
                    remaining = 4 + len + 2;
                }
                else
                {
                    // incompat compat seq sys comp msg(3) payload crc
                    remaining = 8 + len + 2;
                }

                byte[] packet = new byte[2 + remaining];

                packet[0] = stx;
                packet[1] = len;

                int read = reader.Read(packet, 2, remaining);

                if (read != remaining)
                    yield break;

                yield return new TLogRecord
                {
                    Trail = trail,
                    Packet = packet,
                    MavPacket = MavPacket.FromBytes(packet)
                };

                if (reader.BaseStream.Position >= reader.BaseStream.Length)
                    yield break;

                trail = reader.ReadUInt64();

                if (reader.BaseStream.Position >= reader.BaseStream.Length)
                    yield break;

                stx = reader.ReadByte();
            }
        }

    }
}
