using SuperNet.Transport;
using SuperNet.Util;

namespace Net.MessageTypes {
    public class MTShipCourse : IMessage {
        public bool Timed => false;
        public bool Reliable => true;
        public bool Ordered => true;
        public bool Unique => true;
        public byte Channel => (byte)MessageType.ShipCourse;

        public PlayerTag PlayerTag { get; set; }
        public ID ShipID { get; set; }
        public ushort Course { get; set; }

        public void Write(Writer writer) {
            writer.WriteEnum<PlayerTag>(PlayerTag);
            writer.Write(ShipID.ToString());
            writer.Write(Course);
        }

        public void Read(Reader reader) {
            PlayerTag = reader.ReadEnum<PlayerTag>();
            ShipID = new ID(reader.ReadString());
            Course = reader.ReadUInt16();
        }
    }
}