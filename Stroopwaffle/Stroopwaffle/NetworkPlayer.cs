using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using GTA;
using GTA.Math;
using Stroopwaffle_Shared;

namespace Stroopwaffle {
    public class NetworkPlayer {

        public string Name { get; set; }
        public int PlayerID { get; set; }
        public Ped Ped { get; set; }
        public Vector3 Position { get; set; } = new Vector3();
        public bool LocalPlayer { get; set; }
        public Vector3 Rotation { get; set; } = new Vector3();
        public int Aiming { get; set; }
        public Vector3 AimPosition { get; set; } = new Vector3();
        public int Shooting { get; set; }

        public NetworkVehicle NetVehicle { get; set; }

        public bool Walking { get; set; }
        public bool Running { get; set; }
        public bool Sprinting { get; set; }

        public NetworkPlayer() {
            
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
