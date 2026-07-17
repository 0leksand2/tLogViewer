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


        public Heartbeat(MavPacket packet) : base(packet)
        {
            CustomMode = BitConverter.ToUInt32(FullPacket, 0);
            Type = Enum.IsDefined(typeof(AircraftType), (int)FullPacket[4]) ? (AircraftType)FullPacket[4] : AircraftType.Unknown;
            Autopilot = Enum.IsDefined(typeof(Autopilot), (int)FullPacket[5]) ? (Autopilot)FullPacket[5] : Autopilot.Generic;
            BaseMode = (MavModeFlag)FullPacket[6];
            SystemStatus = (SystemStatus)FullPacket[7];
            MavlinkVersion = FullPacket[8];
        }

        public override void Print()
        {
            Console.WriteLine($"Heartbeat Message: CustomMode={CustomMode}, Type={Type}, Autopilot={Autopilot}, Armed={BaseMode.HasFlag(MavModeFlag.SafetyArmed)}, SystemStatus={SystemStatus}");
        }
    }
}
