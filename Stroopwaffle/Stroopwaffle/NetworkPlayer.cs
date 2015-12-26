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

        public bool Walking { get; set; }
        public bool Running { get; set; }
        public bool Sprinting { get; set; }
        public int CurrentWeapon { get; set; }
        public bool Jumping { get; set; }
        public uint Model { get; set; }
        public bool Ragdoll { get; internal set; }
        public bool Reloading { get; internal set; }
        public int Health { get; internal set; }
        public int MaxHealth { get; internal set; }
        public int Armor { get; internal set; }
        public bool Dead { get; internal set; }
        public List<int> Weapons { get; set; } = new List<int>();

        // Clientside Interpolation
        public Vector3 LatestPosition { get; set; } = new Vector3();
        public Vector3 PreviousPosition { get; set; } = new Vector3();
        public DateTime LatestPositionTime { get; set; } = DateTime.Now;

        // Clientside
        public NetworkVehicle NetVehicle { get; set; }

        // Debug clone sync
        public NetworkVehicle CloneVehicle { get; set; }
        public Vector3 VehicleLatestPosition { get; set; } = new Vector3();
        public Vector3 VehiclePreviousPosition { get; set; } = new Vector3();
        public DateTime VehicleLatestPositionTime { get; set; } = DateTime.Now;

        public void CreatePed(Vector3 position, uint pedHash, int WorldRelationship) {
            var characterModel = new Model((PedHash)pedHash);
            characterModel.Request(500);

            // Check the model is valid
            if (characterModel.IsInCdImage && characterModel.IsValid) {
                // If the model isn't loaded, wait until it is
                while (!characterModel.IsLoaded) Script.Wait(100);

                Ped = World.CreatePed(characterModel, position);
            }

            // Delete the model from memory after we've assigned it
            characterModel.MarkAsNoLongerNeeded();

            // DEBUG, not sure how this should be sorted out, reminder clone testing!
            Ped.RelationshipGroup = Game.Player.Character.RelationshipGroup;
            Ped.BlockPermanentEvents = true; 
            Ped.CanRagdoll = true;
            Ped.IsInvincible = true;

            Ped.AddBlip();
            Ped.CurrentBlip.Color = BlipColor.White;
            Ped.CurrentBlip.Scale = 0.8f;
            Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, Ped, true, true); // More control for us, less control for Rockstar
            Ped.RelationshipGroup = WorldRelationship;

            Ped.HasCollision = true;
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
