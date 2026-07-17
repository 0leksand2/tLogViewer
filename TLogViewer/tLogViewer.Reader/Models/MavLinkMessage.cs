namespace tLogViewer.Reader.Models
{
    public abstract class MavLinkMessage
    {
        
        public MavLinkMessage()
        {
            
        }

        public abstract int ExpectedLength { get; }

        public MavPacket Packet { get; protected set; }
        public abstract MavLinkMessage Parse(MavPacket packet);
        public abstract void Print();
    }
}
