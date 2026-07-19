namespace tLogViewer.Core.Models.Messages
{
    public class GpsStatus : MavLinkMessage
    {
        public byte SatellitesVisible;
        public byte[] SatellitePrn = new byte[20];
        public byte[] SatelliteUsed = new byte[20];
        public byte[] SatelliteElevation = new byte[20];
        public byte[] SatelliteAzimuth = new byte[20];
        public byte[] SatelliteSnr = new byte[20];

        public override int ExpectedLength => 101;

        public GpsStatus(MavPacket packet) : base(packet)
        {
            SatellitesVisible = FullPacket[0];
            Array.Copy(FullPacket, 1, SatellitePrn, 0, 20);
            Array.Copy(FullPacket, 21, SatelliteUsed, 0, 20);
            Array.Copy(FullPacket, 41, SatelliteElevation, 0, 20);
            Array.Copy(FullPacket, 61, SatelliteAzimuth, 0, 20);
            Array.Copy(FullPacket, 81, SatelliteSnr, 0, 20);
        }

        public override void Print()
        {
            Console.WriteLine($"GpsStatus: SatellitesVisible={SatellitesVisible}");
        }
    }
}
