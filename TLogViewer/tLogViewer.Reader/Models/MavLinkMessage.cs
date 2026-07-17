namespace tLogViewer.Reader.Models
{
    public abstract class MavLinkMessage
    {
        
        public MavLinkMessage(MavPacket packet)
        {
            Packet = packet;
            FullPacket = new byte[ExpectedLength];

            packet.Payload.CopyTo(FullPacket, 0);
        }

        public abstract int ExpectedLength { get; }
        protected byte[] FullPacket { get; private set; }
        public MavPacket Packet { get; protected set; }

        public abstract void Print();
    }
}
