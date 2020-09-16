using SuperNet.Transport;
using SuperNet.Util;

namespace Net.MessageTypes {
    public class MTShipTarget : IMessage {
        public bool Timed => false;
        public bool Reliable => true;
        public bool Ordered => true;
        public bool Unique => true;
        public byte Channel => (byte)MessageType.ShipTarget;

        public PlayerTag PlayerTag { get; set; }
        public ID ShipID { get; set; }
        public bool HasTarget { get; set; }
        public ID TargetShipID { get; set; } = new ID("");

        public void Write(Writer writer) {
            writer.WriteEnum<PlayerTag>(PlayerTag);
            writer.Write(ShipID.ToString());
            writer.Write(HasTarget);
            writer.Write(TargetShipID.ToString());
        }

        public void Read(Reader reader) {
            PlayerTag = reader.ReadEnum<PlayerTag>();
            ShipID = new ID(reader.ReadString());
            HasTarget = reader.ReadBoolean();
            TargetShipID = new ID(reader.ReadString());
        }
    }
}