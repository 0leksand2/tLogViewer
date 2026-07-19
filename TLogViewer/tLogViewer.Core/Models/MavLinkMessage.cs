namespace tLogViewer.Core.Models
{
    public abstract class MavLinkMessage
    {
        //handling MAVLINK 2 truncation of trailing zeros
        public MavLinkMessage(MavPacket packet)
        {
            Packet = packet;
            FullPacket = new byte[ExpectedLength];

            var copyLength = Math.Min(packet.Payload.Length, ExpectedLength);
            Buffer.BlockCopy(packet.Payload, 0, FullPacket, 0, copyLength);
        }

        public abstract int ExpectedLength { get; }
        protected byte[] FullPacket { get; private set; }
        public MavPacket Packet { get; protected set; }

        public abstract void Print();
    }
}
