namespace tLogViewer.Core.Models.Messages
{
    /// <summary>
    /// ArduPilot RADIO (166) / common RADIO_STATUS (109) — SiK radio link status.
    /// Wire order: rxerrors, fixed, rssi, remrssi, txbuf, noise, remnoise.
    /// </summary>
    public class Radio : MavLinkMessage
    {
        public ushort RxErrors;
        public ushort Fixed;
        public byte Rssi;
        public byte RemRssi;
        public byte TxBufPct;
        public byte Noise;
        public byte RemNoise;

        public override int ExpectedLength => 9;

        public Radio(MavPacket packet) : base(packet)
        {
            RxErrors = BitConverter.ToUInt16(FullPacket, 0);
            Fixed = BitConverter.ToUInt16(FullPacket, 2);
            Rssi = FullPacket[4];
            RemRssi = FullPacket[5];
            TxBufPct = FullPacket[6];
            Noise = FullPacket[7];
            RemNoise = FullPacket[8];
        }

        public override void Print()
        {
            Console.WriteLine(
                $"RADIO: Rssi={Rssi}, RemRssi={RemRssi}, TxBuf={TxBufPct}%, Noise={Noise}, RemNoise={RemNoise}, RxErrors={RxErrors}, Fixed={Fixed}");
        }
    }
}
