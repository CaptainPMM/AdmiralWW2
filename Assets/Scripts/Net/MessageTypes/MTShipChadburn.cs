using SuperNet.Transport;
using SuperNet.Util;
using Ships.ShipSystems;

namespace Net.MessageTypes {
    public class MTShipChadburn : IMessage {
        public bool Timed => false;
        public bool Reliable => true;
        public bool Ordered => false;
        public bool Unique => true;
        public byte Channel => (byte)MessageType.ShipChadburn;

        public PlayerTag PlayerTag { get; set; }
        public ID ShipID { get; set; }
        public Autopilot.ChadburnSetting ChadburnSetting { get; set; }

        public void Write(Writer writer) {
            writer.WriteEnum<PlayerTag>(PlayerTag);
            writer.Write(ShipID.ToString());
            writer.WriteEnum<Autopilot.ChadburnSetting>(ChadburnSetting);
        }

        public void Read(Reader reader) {
            PlayerTag = reader.ReadEnum<PlayerTag>();
            ShipID = new ID(reader.ReadString());
            ChadburnSetting = reader.ReadEnum<Autopilot.ChadburnSetting>();
        }
    }
}