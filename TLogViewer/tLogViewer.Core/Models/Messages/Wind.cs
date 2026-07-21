namespace tLogViewer.Core.Models.Messages
{
    /// <summary>ArduPilot WIND (168) — estimated wind.</summary>
    public class Wind : MavLinkMessage
    {
        /// <summary>Direction the wind is coming from (degrees).</summary>
        public float Direction;

        /// <summary>Wind speed in the ground plane (m/s).</summary>
        public float Speed;

        /// <summary>Vertical wind speed (m/s).</summary>
        public float SpeedZ;

        public double DirectionDeg;
        public double SpeedMS;
        public double SpeedZMS;

        public override int ExpectedLength => 12;

        public Wind(MavPacket packet) : base(packet)
        {
            Direction = BitConverter.ToSingle(FullPacket, 0);
            Speed = BitConverter.ToSingle(FullPacket, 4);
            SpeedZ = BitConverter.ToSingle(FullPacket, 8);

            DirectionDeg = Direction;
            SpeedMS = Speed;
            SpeedZMS = SpeedZ;
        }

        public override void Print()
        {
            Console.WriteLine(
                $"WIND: Direction={DirectionDeg:F1}°, Speed={SpeedMS:F1}m/s, SpeedZ={SpeedZMS:F1}m/s");
        }
    }
}
