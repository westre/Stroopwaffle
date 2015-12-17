using Lidgren.Network;
using Stroopwaffle_Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stroopwaffle_Server {
    public class NetworkPlayer {
        public NetConnection NetConnection { get; set; }
        public int PlayerID { get; set; }
        public Vector3 Position { get; set; }
        public bool SafeForNet { get; set; }
        public Vector3 Rotation { get; set; }
        public int Aiming { get; set; }
        public Vector3 AimLocation { get; set; }
        public int Shooting { get; set; }
        public bool Walking { get; set; }
        public bool Running { get; set; }
        public bool Sprinting { get; set; }

        public NetworkVehicle NetVehicle { get; set; }
        

        public NetworkPlayer(NetConnection netConnection) {
            NetConnection = netConnection;
        }

        public static NetworkPlayer Get(List<NetworkPlayer> players, int id) {
            foreach (NetworkPlayer player in players) {
                if (player.PlayerID == id)
                    return player;
            }
            return null;
        }
    }
}
