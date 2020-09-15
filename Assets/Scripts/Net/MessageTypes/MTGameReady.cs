using SuperNet.Transport;
using SuperNet.Util;

namespace Net.MessageTypes {
    public class MTGameReady : IMessage {
        public bool Timed => false;
        public bool Reliable => true;
        public bool Ordered => false;
        public bool Unique => false;
        public byte Channel => (byte)MessageType.GameReady;

        public void Write(Writer writer) { }
    }
}