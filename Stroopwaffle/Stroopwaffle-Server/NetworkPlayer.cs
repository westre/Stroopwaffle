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
        public Vector3 RunTo { get; set; }
        public int CurrentWeapon { get; set; }

        public NetworkVehicle NetVehicle { get; set; }
        public bool Jumping { get; set; }
        public uint Model { get; set; }
        public bool Visible { get; set; }
        public bool Frozen { get; internal set; }
        public bool Ragdoll { get; internal set; }
        public bool Reloading { get; internal set; }
        public int Health { get; internal set; }
        public int MaxHealth { get; internal set; }
        public int Armor { get; internal set; }
        public bool Dead { get; internal set; }
        public List<int> Weapons { get; set; } = new List<int>();

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
