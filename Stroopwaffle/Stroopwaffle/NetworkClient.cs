using GTA;
using GTA.Math;
using GTA.Native;
using Lidgren.Network;
using Stroopwaffle_Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stroopwaffle {
    public class NetworkClient {
        private Main Main { get; set; }
        public NetClient NetClient { get; set; }

        public List<NetworkPlayer> Players { get; set; }
        public List<NetworkVehicle> Vehicles { get; set; }
        public List<NetworkUI> UIs { get; set; }

        public bool SafeForNet { get; set; }

        private int Switch { get; set; }
        private int BlockAimAtTimer { get; set; }
        private bool BlockAimAtTask { get; set; }

        public int WorldRelationship { get; set; }

        public OOSDetector OOSDetector { get; set; }

        public NetworkClient(Main main) {
            Main = main;

            NetPeerConfiguration config = new NetPeerConfiguration("wh");
            config.AutoFlushSendQueue = false;

            Players = new List<NetworkPlayer>();
            Vehicles = new List<NetworkVehicle>();
            UIs = new List<NetworkUI>();

            OOSDetector = new OOSDetector(this);

            NetClient = new NetClient(config);
            NetClient.Start();
        }

        public NetConnection Connect(string ip) {
            Players.Clear();
            Vehicles.Clear();
            UIs.Clear();

            NetOutgoingMessage hail = NetClient.CreateMessage("This is the hail message");
            return NetClient.Connect(ip, 80, hail);
        }

        public void WritePackets() {
            if(GetLocalPlayer() != null) {
                if (Game.Player.IsAiming) {
                    SendAimingPacket(1);
                }
                else {
                    SendAimingPacket(0);
                }

                if (Game.Player.Character.IsShooting) {
                    SendShootingPacket(1);
                }
                else {
                    SendShootingPacket(0);
                }

                if (Game.Player.Character.CurrentVehicle != null) {
                    bool foundNetworkVehicle = false;
                    foreach (NetworkVehicle networkVehicle in Vehicles) {
                        if (Game.Player.Character.CurrentVehicle.IsInRangeOf(networkVehicle.PhysicalVehicle.Position, 1f)) {
                            SendVehiclePacket(networkVehicle, GetLocalPlayer()); // PlayerID
                            foundNetworkVehicle = true;
                            break;
                        }
                    }

                    if(!foundNetworkVehicle) {
                        OOSDetector.InvalidVehicle(Game.Player.Character.CurrentVehicle);
                    }
                }
                else {
                    SendNoVehiclePacket();
                }

                SendPositionPacket();
                SendRotationPacket();

                if(GetLocalPlayer().Model != (uint)Game.Player.Character.Model.Hash) {
                    OOSDetector.InvalidModel((uint)Game.Player.Character.Model.Hash);
                }

                if (Game.Player.Character.Weapons.Current != null) {
                    SendWeaponPacket();
                }
                
                NetClient.FlushSendQueue();
            }
        }

        private void SendNoVehiclePacket() {
            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage();
            outgoingMessage.Write((byte)PacketType.NoVehicle);
            outgoingMessage.Write(GetLocalPlayer().PlayerID);
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.Unreliable);
        }

        private void SendWeaponPacket() {
            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage();
            outgoingMessage.Write((byte)PacketType.CurrentWeapon);
            outgoingMessage.Write(GetLocalPlayer().PlayerID);
            outgoingMessage.Write((int)Game.Player.Character.Weapons.Current.Hash);
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.Unreliable);
        }

        private void SendVehiclePacket(NetworkVehicle netVehicle, NetworkPlayer netPlayer) {
            Vehicle vehicle = netVehicle.PhysicalVehicle;

            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage();
            outgoingMessage.Write((byte)PacketType.TotalVehicleData);
            outgoingMessage.Write(netVehicle.ID);
            outgoingMessage.Write(netPlayer.PlayerID);
            outgoingMessage.Write(vehicle.Model.Hash);
            outgoingMessage.Write(vehicle.Position.X);
            outgoingMessage.Write(vehicle.Position.Y);
            outgoingMessage.Write(vehicle.Position.Z);
            outgoingMessage.Write(vehicle.Quaternion.W);
            outgoingMessage.Write(vehicle.Quaternion.X);
            outgoingMessage.Write(vehicle.Quaternion.Y);
            outgoingMessage.Write(vehicle.Quaternion.Z);
            outgoingMessage.Write((int)vehicle.PrimaryColor);
            outgoingMessage.Write((int)vehicle.SecondaryColor);
            outgoingMessage.Write(vehicle.Speed);
            outgoingMessage.Write(Utility.GetPedSeat(Game.Player.Character));
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.Unreliable);
        }

        private void SendAimingPacket(int aimState) {
            Vector3 camPosition = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_COORD);
            Vector3 rot = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 0);
            Vector3 dir = Utility.RotationToDirection(rot);
            Vector3 posLookAt = camPosition + dir * 1000f;

            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage();
            outgoingMessage.Write((byte)PacketType.Aiming);
            outgoingMessage.Write(GetLocalPlayer().PlayerID);
            outgoingMessage.Write(aimState);
            outgoingMessage.Write(posLookAt.X);
            outgoingMessage.Write(posLookAt.Y);
            outgoingMessage.Write(posLookAt.Z);
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.Unreliable);
        }

        private void SendPositionPacket() {
            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage();
            Vector3 pos = Game.Player.Character.Position;
            Vector3 rot = Game.Player.Character.Rotation;
            Vector3 dir = Utility.RotationToDirection(rot);
            Vector3 runTo = pos + dir * 1000f;

            outgoingMessage.Write((byte)PacketType.Position);
            outgoingMessage.Write(GetLocalPlayer().PlayerID);

            if (!Main.CloneSync)
                outgoingMessage.Write(Game.Player.Character.Position.X);
            else
                outgoingMessage.Write(Game.Player.Character.Position.X + 1.5f);

            outgoingMessage.Write(Game.Player.Character.Position.Y);

            Vector3 correctPosition = Game.Player.Character.Position - new Vector3(0, 0, 1);
            outgoingMessage.Write(correctPosition.Z); // World.GetGroundHeight(Game.Player.Character.Position)

            outgoingMessage.Write(Game.Player.Character.IsWalking);
            outgoingMessage.Write(Game.Player.Character.IsRunning);
            outgoingMessage.Write(Game.Player.Character.IsSprinting);        
            outgoingMessage.Write(runTo.X);
            outgoingMessage.Write(runTo.Y);
            outgoingMessage.Write(runTo.Z);
            outgoingMessage.Write(Function.Call<bool>(Hash.IS_PED_JUMPING, Game.Player.Character.Handle));

            outgoingMessage.Write(Game.Player.Character.IsRagdoll);
            outgoingMessage.Write(Game.Player.Character.IsReloading);

            outgoingMessage.Write(Game.Player.Character.Health);
            outgoingMessage.Write(Game.Player.Character.MaxHealth);
            outgoingMessage.Write(Game.Player.Character.Armor);
            outgoingMessage.Write(Game.Player.Character.IsDead);

            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.Unreliable);
        }

        private void SendRotationPacket() {
            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage();
            outgoingMessage.Write((byte)PacketType.Rotation);
            outgoingMessage.Write(GetLocalPlayer().PlayerID);
            outgoingMessage.Write(Game.Player.Character.Rotation.X);
            outgoingMessage.Write(Game.Player.Character.Rotation.Y);
            outgoingMessage.Write(Game.Player.Character.Rotation.Z);
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.Unreliable);
        }

        private void SendShootingPacket(int shootState) {
            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage();
            outgoingMessage.Write((byte)PacketType.Shooting);
            outgoingMessage.Write(GetLocalPlayer().PlayerID);
            outgoingMessage.Write(shootState);
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.UnreliableSequenced);
        }

        private void SendRequestInitializationPacket() {
            // Send the directory contents to the server, to block the player if he has malicious files!
            List<string> fileList = new List<string>();
            string[] files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + @"\..\", "*.*", SearchOption.AllDirectories);
            foreach(string file in files) {
                fileList.Add(Path.GetFileName(file));
            }
            string fileString = string.Join(",", fileList);

            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage();
            outgoingMessage.Write((byte)PacketType.Initialization);
            outgoingMessage.Write(fileString);
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendStringMessagePacket(string message) {
            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage();
            outgoingMessage.Write((byte)PacketType.ChatMessage);
            outgoingMessage.Write(GetLocalPlayer().PlayerID);
            outgoingMessage.Write(message);
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
        }

        public void ReadInitializationPacket(NetIncomingMessage netIncomingMessage) {
            int playerId = netIncomingMessage.ReadInt32();
            float posX = netIncomingMessage.ReadFloat();
            float posY = netIncomingMessage.ReadFloat();
            float posZ = netIncomingMessage.ReadFloat();
            bool safeForNet = netIncomingMessage.ReadBoolean();

            // Sets actual playerid
            CreateNetworkPlayer(playerId, posX, posY, posZ, true);

            // Changes the physical location
            Vector3 position = new Vector3(posX, posY, posZ);
            Game.Player.Character.Position = position;

            // Removes all weapons from player
            Game.Player.Character.Weapons.RemoveAll();

            // Set default server synchronized player model
            var characterModel = new Model(PedHash.Brad);
            characterModel.Request(500);

            // Check the model is valid
            if (characterModel.IsInCdImage && characterModel.IsValid) {
                // If the model isn't loaded, wait until it is
                while (!characterModel.IsLoaded) Script.Wait(100);

                // Set the player's model
                Function.Call(Hash.SET_PLAYER_MODEL, Game.Player, characterModel.Hash);
                Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, Game.Player.Character.Handle);

                GetLocalPlayer().Model = (uint)PedHash.Brad;
            }

            // Delete the model from memory after we've assigned it
            characterModel.MarkAsNoLongerNeeded();

            Main.ChatBox.Add("(internal) Allocated PlayerID: " + GetLocalPlayer().PlayerID);
            //Main.ChatBox.Add("(internal) Packet data: " + receivedPacket + ", " + playerId + ", " + posX + ", " + posY + ", " + posZ + ", " + safeForNet);

            WorldRelationship = World.AddRelationshipGroup("AMP_PED");
            World.SetRelationshipBetweenGroups(Relationship.Companion, WorldRelationship, Game.Player.Character.RelationshipGroup); // Make the opposing party ally us (no fleeing)
            World.SetRelationshipBetweenGroups(Relationship.Neutral, Game.Player.Character.RelationshipGroup, WorldRelationship);

            // Sets safe for net flag
            SafeForNet = safeForNet;
        }

        public void ReadDeinitializationPacket(NetIncomingMessage netIncomingMessage) {
            int playerId = netIncomingMessage.ReadInt32();

            // ConcurrentModificationException failsafe
            List<NetworkPlayer> safeServerPlayers = new List<NetworkPlayer>(Players);

            foreach (NetworkPlayer netPlayer in safeServerPlayers) {
                if (netPlayer.PlayerID == playerId) {
                    netPlayer.Ped.CurrentBlip.Remove();
                    netPlayer.Ped.Delete();

                    Players.Remove(netPlayer);
                    Main.ChatBox.Add("(internal) Removed PlayerID: " + playerId);
                }
            }
        }

        public void ReadNoVehiclePacket(NetIncomingMessage netIncomingMessage) {
            int playerId = netIncomingMessage.ReadInt32();

            // We don't want to do ped things on our own player, because we don't have our own physical ped
            if (!Main.CloneSync && GetLocalPlayer().PlayerID == playerId) return;

            NetworkPlayer networkPlayer = null;
            foreach (NetworkPlayer serverNetworkPlayer in Players) {
                if (serverNetworkPlayer.PlayerID == playerId)
                    networkPlayer = serverNetworkPlayer;
            }

            if (networkPlayer.NetVehicle != null) {
                Main.ChatBox.Add("Received NoVehicle Packet, NetVehicle != null, removing Vehicle from Entities: " + networkPlayer.PlayerID);

                NetworkVehicle sourceVehicle = NetworkVehicle.Exists(Vehicles, networkPlayer.NetVehicle.ID);
                sourceVehicle.PlayerID = -1;

                if(!Main.CloneSync) {
                    networkPlayer.Ped.Task.WarpOutOfVehicle(networkPlayer.NetVehicle.PhysicalVehicle);
                }
                else {
                    networkPlayer.Ped.Task.WarpOutOfVehicle(networkPlayer.CloneVehicle.PhysicalVehicle);
                }
                
                networkPlayer.NetVehicle = null;
            }
        }

        public void ReadSetPedPositionPacket(NetIncomingMessage netIncomingMessage) {
            int playerId = netIncomingMessage.ReadInt32();
            float posX = netIncomingMessage.ReadFloat();
            float posY = netIncomingMessage.ReadFloat();
            float posZ = netIncomingMessage.ReadFloat();

            foreach (NetworkPlayer serverNetworkPlayer in Players) {
                if (serverNetworkPlayer.PlayerID == playerId) {
                    serverNetworkPlayer.Position = new Vector3(posX, posY, posZ);

                    if(serverNetworkPlayer.Ped != null)
                        serverNetworkPlayer.Ped.Position = new Vector3(posX, posY, posZ);

                    if (serverNetworkPlayer.LocalPlayer) {
                        Game.Player.Character.Position = new Vector3(posX, posY, posZ);
                    }

                    Main.ChatBox.Add("Setting position to: " + posX + ", " + posY + "," + posZ);
                }
            }
        }

        public void ReadTotalPlayerDataPacket(NetIncomingMessage netIncomingMessage) {
            if (GetLocalPlayer() == null) return;

            int playerId = netIncomingMessage.ReadInt32();
            float posX = netIncomingMessage.ReadFloat();
            float posY = netIncomingMessage.ReadFloat();
            float posZ = netIncomingMessage.ReadFloat();
            float rotX = netIncomingMessage.ReadFloat();
            float rotY = netIncomingMessage.ReadFloat();
            float rotZ = netIncomingMessage.ReadFloat();
            int aiming = netIncomingMessage.ReadInt32();
            float aimPosX = netIncomingMessage.ReadFloat();
            float aimPosY = netIncomingMessage.ReadFloat();
            float aimPosZ = netIncomingMessage.ReadFloat();
            int shooting = netIncomingMessage.ReadInt32();
            bool walking = netIncomingMessage.ReadBoolean();
            bool running = netIncomingMessage.ReadBoolean();
            bool sprinting = netIncomingMessage.ReadBoolean();
            float runToX = netIncomingMessage.ReadFloat();
            float runToY = netIncomingMessage.ReadFloat();
            float runToZ = netIncomingMessage.ReadFloat();
            int weaponHash = netIncomingMessage.ReadInt32();
            bool jumping = netIncomingMessage.ReadBoolean();
            bool visible = netIncomingMessage.ReadBoolean();
            bool frozen = netIncomingMessage.ReadBoolean();
            bool ragdoll = netIncomingMessage.ReadBoolean();
            bool reloading = netIncomingMessage.ReadBoolean();
            int health = netIncomingMessage.ReadInt32();
            int maxHealth = netIncomingMessage.ReadInt32();
            int armor = netIncomingMessage.ReadInt32();
            bool dead = netIncomingMessage.ReadBoolean();
            uint modelHash = (uint)netIncomingMessage.ReadInt32();

            // Process local player, if required
            if (GetLocalPlayer().PlayerID == playerId) {
                ProcessLocalPlayer(visible, frozen);
            }        

            // We don't want to do ped things on our own player, because we don't have our own physical ped
            if (!Main.CloneSync && GetLocalPlayer().PlayerID == playerId) return;

            // Check to see if this player is already in our list
            NetworkPlayer networkPlayer = null;
            foreach (NetworkPlayer serverNetworkPlayer in Players) {
                if (serverNetworkPlayer.PlayerID == playerId)
                    networkPlayer = serverNetworkPlayer;
            }

            // Seems like this is a new player
            if (networkPlayer == null) {
                networkPlayer = CreateNetworkPlayer(playerId, posX, posY, posZ, false);
            }

            // For task animation
            bool walkToRunTransition = false;
            if (!walking && networkPlayer.Walking && running) {
                walkToRunTransition = true;
            }

            // We just started jumping
            if(jumping && !networkPlayer.Jumping) {
                networkPlayer.Ped.Task.Jump();
            }

            // We just started reloading
            if (reloading && !networkPlayer.Reloading) {
                networkPlayer.Ped.Task.ReloadWeapon();
            }

            // We just started ragdolling
            if(ragdoll && !networkPlayer.Ragdoll) {
                Function.Call(Hash.SET_PED_TO_RAGDOLL, networkPlayer.Ped, 10000, 10000, 0, 1, 1, 0);
                Function.Call(Hash.CREATE_NM_MESSAGE, 1, 466);
                Function.Call(Hash.GIVE_PED_NM_MESSAGE, networkPlayer.Ped);
            }

            // We have a new model!
            if (networkPlayer.Model != modelHash) {
                networkPlayer.Model = modelHash;
                SetPlayerPed(networkPlayer, modelHash);
            }

            networkPlayer.Aiming = aiming;
            networkPlayer.AimPosition = new Vector3(aimPosX, aimPosY, aimPosZ);
            networkPlayer.Shooting = shooting;
            networkPlayer.Walking = walking;
            networkPlayer.Running = running;
            networkPlayer.Sprinting = sprinting;
            networkPlayer.RunTo = new Vector3(runToX, runToY, runToZ);
            networkPlayer.CurrentWeapon = weaponHash;
            networkPlayer.Jumping = jumping;
            networkPlayer.Ragdoll = ragdoll;
            networkPlayer.Reloading = reloading;
            networkPlayer.Health = health;
            networkPlayer.MaxHealth = maxHealth;
            networkPlayer.Armor = armor;
            networkPlayer.Dead = dead;

            networkPlayer.Ped.IsVisible = visible;
            networkPlayer.Ped.FreezePosition = frozen;

            // Update the player if he's not in a vehicle
            if (networkPlayer.NetVehicle == null) {
                if (networkPlayer.Shooting == 1) {
                    Function.Call(Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS, networkPlayer.Ped.Position.X, networkPlayer.Ped.Position.Y, networkPlayer.Ped.Position.Z, networkPlayer.AimPosition.X, networkPlayer.AimPosition.Y, networkPlayer.AimPosition.Z, 10, true, networkPlayer.CurrentWeapon, networkPlayer.Ped, true, false, 100);
                    //World.ShootBullet(networkPlayer.Position, networkPlayer.AimPosition, networkPlayer.Ped, networkPlayer.CurrentWeapon, 10);
                    
                    networkPlayer.Ped.ShootRate = 100000;
                    networkPlayer.Ped.Task.ShootAt(networkPlayer.AimPosition, 355);

                    BlockAimAtTask = true;
                }

                if (networkPlayer.Aiming == 1 && networkPlayer.Shooting == 0 && !BlockAimAtTask) {
                    //Function.Call(Hash.TASK_GO_TO_COORD_WHILE_AIMING_AT_COORD, networkPlayer.Ped.Handle, networkPlayer.Position.X, networkPlayer.Position.Y, networkPlayer.Position.Z, networkPlayer.AimPosition.X, networkPlayer.AimPosition.Y, networkPlayer.AimPosition.Z, 2f, 0, 0x3F000000, 0x40800000, 1, 512, 0, (uint)FiringPattern.FullAuto);
                    networkPlayer.Ped.Task.AimAt(networkPlayer.AimPosition, 200);
                }

                if (networkPlayer.Aiming == 0 && networkPlayer.Shooting == 0 && !networkPlayer.Jumping) {
                    if (walkToRunTransition) {
                        // This is really ugly, animation-wise, TODO check if Task.RunTo changes walk/run animation depending on the distance from RunTo
                        networkPlayer.Ped.Task.ClearAllImmediately();
                        networkPlayer.Ped.Task.RunTo(networkPlayer.RunTo, true, 10);
                    }
                    else if ((networkPlayer.Running || networkPlayer.Sprinting) && !networkPlayer.Walking) {
                        networkPlayer.Ped.Task.RunTo(networkPlayer.RunTo, true, 10);
                    }
                    else if (networkPlayer.Walking) {
                        networkPlayer.Ped.Task.GoTo(networkPlayer.RunTo, true, 10);
                    }
                    else {
                        networkPlayer.Ped.Task.ClearAll();
                        networkPlayer.Ped.Rotation = new Vector3(rotX, rotY, rotZ);
                    }
                }

                // Set current weapon
                if(networkPlayer.Ped.Weapons.Current.Hash != (WeaponHash)networkPlayer.CurrentWeapon) {
                    Weapon weapon = networkPlayer.Ped.Weapons.Give((WeaponHash)networkPlayer.CurrentWeapon, 9999, true, true);
                    networkPlayer.Ped.Weapons.Select(weapon);
                }

                if(!Main.Interpolation) {
                    if (!networkPlayer.Ragdoll) {
                        networkPlayer.Ped.Position = new Vector3(posX, posY, posZ);
                    }
                    else {
                        networkPlayer.Ped.Position = new Vector3(posX, posY, World.GetGroundHeight(new Vector3(posX, posY, posZ)));
                    }
                }
                else {
                    networkPlayer.LatestPosition = new Vector3(posX, posY, posZ);

                    DateTime dt = DateTime.Now;
                    dt = dt.AddMilliseconds((float)Rate.PedInterpolation);

                    networkPlayer.LatestPositionTime = dt;
                    networkPlayer.PreviousPosition = networkPlayer.Ped.Position;
                }           
            }

            networkPlayer.Position = new Vector3(posX, posY, posZ);

            if (BlockAimAtTask) {
                BlockAimAtTimer++;
                if (BlockAimAtTimer == 33) {
                    BlockAimAtTimer = 0;
                    BlockAimAtTask = false;
                }
            }
            Switch++;
        }

        private void ProcessLocalPlayer(bool visible, bool frozen) {
            Game.Player.Character.IsVisible = visible;
            Game.Player.Character.FreezePosition = frozen;
        }

        public void ReadTotalVehicleDataPacket(NetIncomingMessage netIncomingMessage) {
            if (GetLocalPlayer() == null) return;

            int id = netIncomingMessage.ReadInt32();
            int playerId = netIncomingMessage.ReadInt32();
            int vehicleHash = netIncomingMessage.ReadInt32();
            float posX = netIncomingMessage.ReadFloat();
            float posY = netIncomingMessage.ReadFloat();
            float posZ = netIncomingMessage.ReadFloat();
            float rotW = netIncomingMessage.ReadFloat();
            float rotX = netIncomingMessage.ReadFloat();
            float rotY = netIncomingMessage.ReadFloat();
            float rotZ = netIncomingMessage.ReadFloat();
            int primaryColor = netIncomingMessage.ReadInt32();
            int secondaryColor = netIncomingMessage.ReadInt32();
            float speed = netIncomingMessage.ReadFloat();
            int seat = netIncomingMessage.ReadInt32();

            NetworkVehicle networkVehicle = NetworkVehicle.Exists(Vehicles, id);

            // Clone debug sync
            NetworkPlayer networkPlayer = NetworkPlayer.Get(Players, playerId);
            if(networkPlayer != null && Main.CloneSync) {
                if(networkPlayer.CloneVehicle == null) {
                    networkPlayer.CloneVehicle = new NetworkVehicle();

                    networkPlayer.CloneVehicle.PhysicalVehicle = World.CreateVehicle(vehicleHash, new Vector3(posX + 5, posY, posZ));
                    networkPlayer.CloneVehicle.PhysicalVehicle.Quaternion = new Quaternion(rotX, rotY, rotZ, rotW);
                    networkPlayer.CloneVehicle.PhysicalVehicle.CanTiresBurst = false;
                    networkPlayer.CloneVehicle.PhysicalVehicle.PlaceOnGround();

                    // Testing ;)
                    networkPlayer.CloneVehicle.PhysicalVehicle.CanBeVisiblyDamaged = false;
                    networkPlayer.CloneVehicle.PhysicalVehicle.EngineRunning = true;
                    networkPlayer.CloneVehicle.PhysicalVehicle.PrimaryColor = (VehicleColor)primaryColor;
                    networkPlayer.CloneVehicle.PhysicalVehicle.SecondaryColor = (VehicleColor)secondaryColor;
                    Main.ChatBox.Add("Created new debug vehicle");
                }

                // Interpolation
                networkPlayer.VehicleLatestPosition = new Vector3(posX + 5, posY, posZ);

                DateTime dt = DateTime.Now;
                dt = dt.AddMilliseconds((float)Rate.VehicleInterpolation);

                networkPlayer.VehicleLatestPositionTime = dt;
                networkPlayer.VehiclePreviousPosition = networkPlayer.CloneVehicle.PhysicalVehicle.Position;
                networkPlayer.CloneVehicle.PhysicalVehicle.Quaternion = new Quaternion(rotX, rotY, rotZ, rotW);

                if (!Main.Interpolation) {
                    networkPlayer.CloneVehicle.PhysicalVehicle.Position = new Vector3(posX, posY, posZ) + new Vector3(5, 0, 0);
                    networkPlayer.CloneVehicle.PhysicalVehicle.Quaternion = new Quaternion(rotX, rotY, rotZ, rotW);
                }
            }

            // It exists in our list
            if (networkVehicle == null) {
                networkVehicle = new NetworkVehicle();

                // Client side only!
                networkVehicle.PhysicalVehicle = World.CreateVehicle(vehicleHash, new Vector3(posX, posY, posZ));
                networkVehicle.PhysicalVehicle.Quaternion = new Quaternion(rotX, rotY, rotZ, rotW);
                networkVehicle.PhysicalVehicle.CanTiresBurst = false;
                networkVehicle.PhysicalVehicle.PlaceOnGround();
                networkVehicle.PhysicalVehicle.NumberPlate = "ID: " + id;
                Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, networkVehicle.PhysicalVehicle, true, true); // More control for us, less control for Rockstar

                // Testing ;)
                networkVehicle.PhysicalVehicle.CanBeVisiblyDamaged = false;
                networkVehicle.PhysicalVehicle.IsInvincible = true;
                networkVehicle.PhysicalVehicle.EngineRunning = true;
                networkVehicle.PhysicalVehicle.PrimaryColor = (VehicleColor)primaryColor;
                networkVehicle.PhysicalVehicle.SecondaryColor = (VehicleColor)secondaryColor;

                Vehicles.Add(networkVehicle);
                Main.ChatBox.Add("(internal) Added new vehicle id: " + id);
            }

            // -1 to playerId, or other playerId means someone entered the vehicle!
            if (networkVehicle.PlayerID != playerId) {
                NetworkPlayer driver = NetworkPlayer.Get(Players, playerId);
                if (driver != null) {
                    if(!Main.CloneSync) {
                        driver.Ped?.Task.WarpIntoVehicle(networkVehicle.PhysicalVehicle, (VehicleSeat)seat); // If we don't have a ped, i.e. localplayer
                    }
                    else {
                        driver.Ped.Task.WarpIntoVehicle(driver.CloneVehicle.PhysicalVehicle, (VehicleSeat)seat);
                    }   
                              
                    driver.NetVehicle = networkVehicle;
                    Main.ChatBox.Add("(internal) Set driver ped in vehicle: " + networkVehicle.PhysicalVehicle.Model.ToString());
                }
            }

            if (networkVehicle.PlayerID != GetLocalPlayer().PlayerID) {
                //var distance = new Vector3(posX, posY, posZ) - networkVehicle.PhysicalVehicle.Position;
                //distance.Normalize();

                networkVehicle.PhysicalVehicle.Position = new Vector3(posX, posY, posZ);
                networkVehicle.PhysicalVehicle.Quaternion = new Quaternion(rotX, rotY, rotZ, rotW);

                //if (speed >= 1 && (Switch % 3 == 0)) {
                //    var direction = distance * Math.Abs(speed - networkVehicle.PhysicalVehicle.Speed);
                //    networkVehicle.PhysicalVehicle.ApplyForce(direction);
                //}
            }

            // No driver, so just force the physical vehicle's position to the actual position in order to prevent desync between physical and virtual data
            if (networkVehicle.PlayerID == -1) {
                networkVehicle.PhysicalVehicle.Position = new Vector3(posX, posY, posZ);
                networkVehicle.PhysicalVehicle.Quaternion = new Quaternion(rotX, rotY, rotZ, rotW);
            }

            networkVehicle.ID = id;
            networkVehicle.PlayerID = playerId;
            networkVehicle.Hash = vehicleHash;
            networkVehicle.PosX = posX;
            networkVehicle.PosY = posY;
            networkVehicle.PosZ = posZ;
            networkVehicle.RotW = rotW;
            networkVehicle.RotX = rotX;
            networkVehicle.RotY = rotY;
            networkVehicle.RotZ = rotZ;
            networkVehicle.PrimaryColor = primaryColor;
            networkVehicle.SecondaryColor = secondaryColor;
            networkVehicle.Speed = speed;
        }

        public void ReadPackets() {
            NetIncomingMessage netIncomingMessage;

            while ((netIncomingMessage = NetClient.ServerConnection.Peer.ReadMessage()) != null) {
                if (netIncomingMessage.MessageType == NetIncomingMessageType.StatusChanged) {
                    NetConnectionStatus status = (NetConnectionStatus)netIncomingMessage.ReadByte();

                    if (status == NetConnectionStatus.Connected) {
                        SendRequestInitializationPacket();
                        NetClient.FlushSendQueue();
                    }
                }
                else if(netIncomingMessage.MessageType == NetIncomingMessageType.Data) {
                    PacketType receivedPacket = (PacketType)netIncomingMessage.ReadByte();

                    if (receivedPacket == PacketType.Initialization) {
                        ReadInitializationPacket(netIncomingMessage);
                    }
                    else if (receivedPacket == PacketType.Deinitialization) {
                        ReadDeinitializationPacket(netIncomingMessage);                       
                    }
                    else if (receivedPacket == PacketType.ChatMessage) {
                        string message = netIncomingMessage.ReadString();

                        Main.ChatBox.Add(message);
                    }
                    else if (receivedPacket == PacketType.NoVehicle) {
                        ReadNoVehiclePacket(netIncomingMessage);
                    }
                    else if (receivedPacket == PacketType.TotalPlayerData) {
                        ReadTotalPlayerDataPacket(netIncomingMessage);
                    }
                    else if(receivedPacket == PacketType.TotalVehicleData) {
                        ReadTotalVehicleDataPacket(netIncomingMessage);
                    }
                    else if (receivedPacket == PacketType.SetPedPosition) {
                        ReadSetPedPositionPacket(netIncomingMessage);
                    }
                    else if (receivedPacket == PacketType.SetPlayerArmor) {
                        ReadSetPlayerArmorPacket(netIncomingMessage);
                    }
                    else if (receivedPacket == PacketType.SetPlayerHealth) {
                        ReadSetPlayerHealthPacket(netIncomingMessage);
                    }
                    else if(receivedPacket == PacketType.NewPed) {
                        ReadNewPedPacket(netIncomingMessage);
                    }
                    else if (receivedPacket == PacketType.GivePlayerWeapon) {
                        ReadGivePlayerWeaponPacket(netIncomingMessage);
                    }
                    else if (receivedPacket == PacketType.SetPlayerModel) {
                        ReadSetPlayerModelPacket(netIncomingMessage);
                    }
                    else if (receivedPacket == PacketType.GUIPacket) {
                        ReadGUIPacket(netIncomingMessage);
                    }
                }
                NetClient.Recycle(netIncomingMessage);
            }
        }

        private void ReadGUIPacket(NetIncomingMessage netIncomingMessage) {
            GUIPacket receivedGUIPacket = (GUIPacket)netIncomingMessage.ReadByte();
            int id = netIncomingMessage.ReadInt32();
            int playerId = netIncomingMessage.ReadInt32();
            int posX = netIncomingMessage.ReadInt32();
            int posY = netIncomingMessage.ReadInt32();
            int sizeX = netIncomingMessage.ReadInt32();
            int sizeY = netIncomingMessage.ReadInt32();
            byte r = netIncomingMessage.ReadByte();
            byte g = netIncomingMessage.ReadByte();
            byte b = netIncomingMessage.ReadByte();
            byte a = netIncomingMessage.ReadByte();

            // Check to see if this UI is already in our list
            NetworkUI networkUi = null;
            foreach (NetworkUI ui in UIs) {
                if (ui.ID == id)
                    networkUi = ui;
            }

            // It doesn't exist, so create one!
            if(networkUi == null) {
                networkUi = new NetworkUI();
                UIs.Add(networkUi);

                Main.ChatBox.Add("Created new NUI");
            }

            if (receivedGUIPacket == GUIPacket.Rectangle) {
                networkUi.ID = id;
                networkUi.Type = (byte)receivedGUIPacket;
                networkUi.PlayerId = playerId;
                networkUi.PosX = posX;
                networkUi.PosY = posY;
                networkUi.SizeX = sizeX;
                networkUi.SizeY = sizeY;
                networkUi.R = r;
                networkUi.B = b;
                networkUi.G = g;
                networkUi.A = a;
                networkUi.UIElement = new UIRectangle(new Point(networkUi.PosX, networkUi.PosY), new Size(networkUi.SizeX, networkUi.SizeY), Color.FromArgb(networkUi.A, networkUi.R, networkUi.G, networkUi.B));
            }
        }

        private void ReadSetPlayerModelPacket(NetIncomingMessage netIncomingMessage) {
            int playerId = netIncomingMessage.ReadInt32();
            uint modelId = (uint)netIncomingMessage.ReadInt32();

            foreach (NetworkPlayer serverNetworkPlayer in Players) {
                if (serverNetworkPlayer.PlayerID == playerId) {
                    serverNetworkPlayer.Model = modelId;

                    var characterModel = new Model((PedHash)serverNetworkPlayer.Model);
                    characterModel.Request(500);

                    if (serverNetworkPlayer.LocalPlayer) {
                        // Check the model is valid
                        if (characterModel.IsInCdImage && characterModel.IsValid) {
                            // If the model isn't loaded, wait until it is
                            while (!characterModel.IsLoaded) Script.Wait(100);

                            // Set the player's model
                            Function.Call(Hash.SET_PLAYER_MODEL, Game.Player, characterModel.Hash);
                            Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, Game.Player.Character.Handle);
                        }

                        Main.ChatBox.Add("DEBUG: Model has been set to: " + modelId);
                    }

                    // We don't want to do ped things on our own player, because we don't have our own physical ped
                    if (Main.CloneSync || GetLocalPlayer().PlayerID != playerId) {
                        SetPlayerPed(serverNetworkPlayer, serverNetworkPlayer.Model);
                        //Function.Call(Hash.SET_PLAYER_MODEL, serverNetworkPlayer.Ped, serverNetworkPlayer.Model);
                    }

                    // Delete the model from memory after we've assigned it
                    characterModel.MarkAsNoLongerNeeded();
                }
            }
        }

        private void ReadGivePlayerWeaponPacket(NetIncomingMessage netIncomingMessage) {
            int playerId = netIncomingMessage.ReadInt32();
            int weaponId = netIncomingMessage.ReadInt32();

            foreach (NetworkPlayer serverNetworkPlayer in Players) {
                if (serverNetworkPlayer.PlayerID == playerId) {
                    serverNetworkPlayer.Weapons.Add(weaponId);

                    if (serverNetworkPlayer.LocalPlayer) {
                        Weapon weapon = Game.Player.Character.Weapons.Give((WeaponHash)weaponId, 9999, true, true);

                        Main.ChatBox.Add("DEBUG: You have been given: " + weapon.Hash);
                    }

                    // We don't want to do ped things on our own player, because we don't have our own physical ped
                    if (Main.CloneSync || GetLocalPlayer().PlayerID != playerId) {
                        Weapon weapon = serverNetworkPlayer.Ped.Weapons.Give((WeaponHash)weaponId, 9999, true, true);
                    }
                }
            }
        }

        private void ReadNewPedPacket(NetIncomingMessage netIncomingMessage) {
            int playerId = netIncomingMessage.ReadInt32();

            foreach (NetworkPlayer serverNetworkPlayer in Players) {
                if (serverNetworkPlayer.PlayerID == playerId) {

                    // We don't want to do ped things on our own player, because we don't have our own physical ped
                    if (!Main.CloneSync && GetLocalPlayer().PlayerID == playerId) return;

                    serverNetworkPlayer.Ped.CurrentBlip.Remove();
                    serverNetworkPlayer.CreatePed(serverNetworkPlayer.Position, serverNetworkPlayer.Model, WorldRelationship);
                }
            }
        }

        public void ReadSetPlayerArmorPacket(NetIncomingMessage netIncomingMessage) {
            int playerId = netIncomingMessage.ReadInt32();
            int armor = netIncomingMessage.ReadInt32();

            foreach (NetworkPlayer serverNetworkPlayer in Players) {
                if (serverNetworkPlayer.PlayerID == playerId) {
                    serverNetworkPlayer.Armor = armor;

                    if (serverNetworkPlayer.LocalPlayer) {
                        Game.Player.Character.Armor = armor;
                    }
                }
            }
        }

        public void ReadSetPlayerHealthPacket(NetIncomingMessage netIncomingMessage) {
            int playerId = netIncomingMessage.ReadInt32();
            int health = netIncomingMessage.ReadInt32();

            foreach (NetworkPlayer serverNetworkPlayer in Players) {
                if (serverNetworkPlayer.PlayerID == playerId) {
                    serverNetworkPlayer.Health = health;

                    if (health <= 0) {
                        // We don't want to do ped things on our own player, because we don't have our own physical ped
                        if (Main.CloneSync || GetLocalPlayer().PlayerID != playerId)
                            serverNetworkPlayer.Ped.Kill();
                    }

                    if (serverNetworkPlayer.LocalPlayer) {
                        Game.Player.Character.Health = health;

                        if (health <= 0) {
                            Game.Player.Character.Kill();
                        }
                    }
                }
            }
        }

        public void Disconnect() {
            foreach (NetworkPlayer serverNetworkPlayer in Players) {
                if(serverNetworkPlayer.Ped != null) {
                    serverNetworkPlayer.Ped.CurrentBlip.Remove();
                    serverNetworkPlayer.Ped.Delete();
                }  
            }
            NetClient.Disconnect("bye");
        }

        public NetworkPlayer GetLocalPlayer() {
            if(Players != null) {
                foreach (NetworkPlayer networkPlayer in Players) {
                    if (networkPlayer.LocalPlayer)
                        return networkPlayer;
                }
            }
            else {
                Main.ChatBox.Add("SP is Null?");
            }
            
            return null;
        }

        private NetworkPlayer CreateNetworkPlayer(int playerId, float posX, float posY, float posZ, bool localPlayer) {
            NetworkPlayer networkPlayer = new NetworkPlayer();
            networkPlayer.PlayerID = playerId;
            networkPlayer.Position = new Vector3(posX, posY, posZ);
            networkPlayer.LocalPlayer = localPlayer;

            // Create the physical ped
            if(!localPlayer || Main.CloneSync) {
                networkPlayer.CreatePed(networkPlayer.Position, (uint)PedHash.Brad, WorldRelationship);
            }
                  
            Players.Add(networkPlayer);

            Main.ChatBox.Add("(internal) Added PlayerID " + networkPlayer.PlayerID + " to the ServerPlayers list");

            return networkPlayer;
        }

        private void SetPlayerPed(NetworkPlayer networkPlayer, uint modelHash) {
            if (networkPlayer.Ped != null) {
                networkPlayer.Ped.Delete();
                networkPlayer.CreatePed(networkPlayer.Position, modelHash, WorldRelationship);
            }
            else {
                Main.ChatBox.Add("Error: SetPlayerPed Ped is NULL");
            }
        }
    }
}
