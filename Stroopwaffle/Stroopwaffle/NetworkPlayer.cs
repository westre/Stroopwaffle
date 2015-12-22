using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using GTA;
using GTA.Math;
using Stroopwaffle_Shared;
using GTA.Native;

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
        public Vector3 RunTo { get; set; } = new Vector3();

        public NetworkVehicle NetVehicle { get; set; }

        public bool Walking { get; set; }
        public bool Running { get; set; }
        public bool Sprinting { get; set; }
        public int CurrentWeapon { get; set; }
        public bool Jumping { get; set; }
        public int Model { get; set; }

        public NetworkPlayer() {
            
        }

        public void CreatePed(Vector3 position, Model pedHash, int WorldRelationship) {
            Ped = World.CreatePed(pedHash, position);
            Ped.Weapons.Give(WeaponHash.Pistol, 500, true, true);

            // DEBUG, not sure how this should be sorted out, reminder clone testing!
            Ped.RelationshipGroup = Game.Player.Character.RelationshipGroup;
            Ped.BlockPermanentEvents = true;
            Ped.CanRagdoll = false;
            Ped.IsInvincible = true;
            Ped.AddBlip();
            Ped.CurrentBlip.Color = BlipColor.White;
            Ped.CurrentBlip.Scale = 0.8f;
            Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, Ped, true, true); // More control for us, less control for Rockstar
            Ped.RelationshipGroup = WorldRelationship;
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
