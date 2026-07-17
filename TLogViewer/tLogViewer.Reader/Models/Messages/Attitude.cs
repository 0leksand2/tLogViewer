using System;
using System.Collections.Generic;
using System.Text;

namespace tLogViewer.Reader.Models.Messages
{
    public class Attitude : MavLinkMessage
    {
        public uint TimeBootMs;
        public float Roll;
        public float Pitch;
        //deg
        public float Yaw;
        //deg
        public float RollSpeed;
        //deg
        public float PitchSpeed;
        public float YawSpeed;

        public double RollDeg;
        public double PitchDeg;
        public double YawDeg;

        public override int ExpectedLength => 28;

        public Attitude(MavPacket packet) : base(packet)
        {
            TimeBootMs = BitConverter.ToUInt32(FullPacket, 0);
            Roll = BitConverter.ToSingle(FullPacket, 4);
            Pitch = BitConverter.ToSingle(FullPacket, 8);
            Yaw = BitConverter.ToSingle(FullPacket, 12);
            RollSpeed = BitConverter.ToSingle(FullPacket, 16);
            PitchSpeed = BitConverter.ToSingle(FullPacket, 20);
            YawSpeed = BitConverter.ToSingle(FullPacket, 24);

            RollDeg = Roll * 180.0 / Math.PI;
            PitchDeg = Pitch * 180.0 / Math.PI;
            YawDeg = Yaw * 180.0 / Math.PI;
        }
        public override void Print()
        {
            Console.WriteLine($"Attitude message: Roll={RollDeg:F2}, Pitch={PitchDeg:F2}, Yaw={YawDeg:F2}");
        }
    }
}
