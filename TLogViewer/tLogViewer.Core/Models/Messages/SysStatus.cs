using tLogViewer.Core.Enums.SysStatus;

namespace tLogViewer.Core.Models.Messages
{
    public class SysStatus : MavLinkMessage
    {
        public MavSysSensor SensorsPresent;
        public MavSysSensor SensorsEnabled;
        public MavSysSensor SensorsHealth;
        public ushort Load;
        public ushort BatteryVoltage;
        public short BatteryCurrent;
        public sbyte BatteryRemaining;

        public ushort DropRateComm;
        public ushort ErrorsComm;
        public ushort Errors1;
        public ushort Errors2;
        public ushort Errors3;
        public ushort Errors4;

        public override int ExpectedLength => 31;

        public SysStatus(MavPacket packet) : base(packet)
        {
            SensorsPresent = (MavSysSensor)BitConverter.ToUInt32(FullPacket, 0);
            SensorsEnabled = (MavSysSensor)BitConverter.ToUInt32(FullPacket, 4);
            SensorsHealth = (MavSysSensor)BitConverter.ToUInt32(FullPacket, 8);

            Load = BitConverter.ToUInt16(FullPacket, 12);
            BatteryVoltage = BitConverter.ToUInt16(FullPacket, 14);
            BatteryCurrent = BitConverter.ToInt16(FullPacket, 16);
            BatteryRemaining = unchecked((sbyte)FullPacket[18]);

            DropRateComm = BitConverter.ToUInt16(FullPacket, 19);
            ErrorsComm = BitConverter.ToUInt16(FullPacket, 21);

            Errors1 = BitConverter.ToUInt16(FullPacket, 23);
            Errors2 = BitConverter.ToUInt16(FullPacket, 25);
            Errors3 = BitConverter.ToUInt16(FullPacket, 27);
            Errors4 = BitConverter.ToUInt16(FullPacket, 29);
        }

        public override void Print()
        {
            Console.WriteLine($"System Status: Gyro Present: {SensorsPresent.HasFlag(MavSysSensor.GPS)}, Gyro Enabled: {SensorsEnabled.HasFlag(MavSysSensor.GPS)}, Gyro Health: {SensorsHealth.HasFlag(MavSysSensor.GPS)}");
        }
    }
}
