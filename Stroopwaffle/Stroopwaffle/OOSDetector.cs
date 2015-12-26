using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using Lidgren.Network;
using Stroopwaffle_Shared;

namespace Stroopwaffle {
    public class OOSDetector {
        private NetworkClient NetworkClient { get; set; }

        public OOSDetector(NetworkClient networkClient) {
            NetworkClient = networkClient;
        }

        // TODO send the extra parameters
        public void InvalidVehicle(Vehicle currentVehicle) {
            SendOOSPacket(OOSPacket.InvalidVehicle);
        }

        public void InvalidModel(uint modelHash) {
            SendOOSPacket(OOSPacket.InvalidModel);
        }

        private void SendOOSPacket(OOSPacket oosPacket) {
            NetOutgoingMessage outgoingMessage = NetworkClient.NetClient.CreateMessage();
            outgoingMessage.Write((byte)PacketType.OOSPacket);
            outgoingMessage.Write((byte)oosPacket);
            outgoingMessage.Write(NetworkClient.GetLocalPlayer().PlayerID);
            NetworkClient.NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
        }
    }
}
