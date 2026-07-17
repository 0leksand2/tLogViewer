using tLogViewer.Core.Enums;

namespace tLogViewer.Core.Models
{
    public class MavPacket
    {
        public MavLinkVersion Version { get; set; }

        //first byte determines the version of the packet
        public byte Stx { get; set; }
        //length of the payload
        public int Length { get; set; }
        //sequence number of the packet
        public byte Seq { get; set; }
        public byte SysId { get; set; }
        public byte CompId { get; set; }
        public MavMessageTypeId MsgId { get; set; }
        public byte[] Payload { get; set; }
        public ushort Crc { get; set; }

        public static MavPacket FromBytes(byte[] packet) 
        {
            if (packet.Length < 8)
                throw new InvalidDataException("Packet too short.");

            var result = new MavPacket();
            result.Version = packet[0] == 0xFE ? MavLinkVersion.MavLink1 : MavLinkVersion.MavLink2;
            result.Stx = packet[0];
            result.Length = packet[1];

            if (result.Version == MavLinkVersion.MavLink1)
            {
                // MAVLink 1
                result.Seq = packet[2];
                result.SysId = packet[3];
                result.CompId = packet[4];

                result.MsgId = (MavMessageTypeId)packet[5];

                result.Payload = new byte[result.Length];
                Array.Copy(packet, 6, result.Payload, 0, result.Length);

                result.Crc = BitConverter.ToUInt16(packet, 6 + result.Length);
            }
            else if (result.Version == MavLinkVersion.MavLink2)
            {
                // MAVLink 2
                result.Seq = packet[4];
                result.SysId = packet[5];
                result.CompId = packet[6];

                result.MsgId =
                    (MavMessageTypeId)(uint)(
                        packet[7] |
                        (packet[8] << 8) |
                        (packet[9] << 16));

                result.Payload = new byte[result.Length];
                Array.Copy(packet, 10, result.Payload, 0, result.Length);

                result.Crc = BitConverter.ToUInt16(packet, 10 + result.Length);
            }

            return result;
        }
}}
