using SuperNet.Transport;
using SuperNet.Util;

namespace Net.MessageTypes {
    public class MTTest : IMessage {
        public bool Timed => false;
        public bool Reliable => true;
        public bool Ordered => false;
        public bool Unique => true;
        public byte Channel => (byte)MessageType.Test;

        public string Msg { get; set; } = "Hello World";

        public void Write(Writer writer) {
            writer.Write(Msg);
        }

        public void Read(Reader reader) {
            Msg = reader.ReadString();
        }
    }
}