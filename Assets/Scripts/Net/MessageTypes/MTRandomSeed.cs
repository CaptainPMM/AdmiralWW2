using SuperNet.Transport;
using SuperNet.Util;

namespace Net.MessageTypes {
    public class MTRandomSeed : IMessage {
        public bool Timed => false;
        public bool Reliable => false;
        public bool Ordered => true;
        public bool Unique => true;
        public byte Channel => (byte)MessageType.RandomSeed;

        public int Seed { get; set; }

        public void Write(Writer writer) {
            writer.Write(Seed);
        }

        public void Read(Reader reader) {
            Seed = reader.ReadInt32();
        }
    }
}