using tLogViewer.Reader.Enums.Heartbeat;

namespace tLogViewer.Reader.Models.Messages
{
    public class Heartbeat : MavLinkMessage
    {
        //do not handle custom mode for now, might differ on autopilot type
        public uint CustomMode;

        public AircraftType Type;

        public Autopilot Autopilot;

        public MavModeFlag BaseMode;

        public SystemStatus SystemStatus;

        public byte MavlinkVersion;

        public override int ExpectedLength => 9;

        public Heartbeat() : base()
        {
        }

        public override MavLinkMessage Parse(MavPacket packet)
        {
            byte[] p = new byte[ExpectedLength];

            packet.Payload.CopyTo(p, 0);
            var type = Enum.IsDefined(typeof(AircraftType), (int)p[4]) ? (AircraftType)p[4] : AircraftType.Unknown;

            //do not handle unkown for now
            if (type == AircraftType.Unknown)
                return null;
            return new Heartbeat()
            {
                CustomMode = BitConverter.ToUInt32(p, 0),
                Type = type,
                Autopilot = Enum.IsDefined(typeof(Autopilot), (int)p[5]) ? (Autopilot)p[5] : Autopilot.Generic,
                BaseMode = (MavModeFlag)p[6],
                SystemStatus = (SystemStatus)p[7],
                MavlinkVersion = p[8],
                Packet = packet
            };
        }

        public override void Print()
        {
            Console.WriteLine($"Heartbeat Message: CustomMode={CustomMode}, Type={Type}, Autopilot={Autopilot}, Armed={BaseMode.HasFlag(MavModeFlag.SafetyArmed)}, SystemStatus={SystemStatus}");
        }
    }
}
