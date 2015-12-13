﻿using GTA;
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
        public int PlayerID { get; set; } = -1;
        private List<NetworkPlayer> ServerPlayers { get; set; }
        public bool SafeForNet { get; set; }

        private int Switch { get; set; }
        private int BlockAimAtTimer { get; set; }
        private bool BlockAimAtTask { get; set; }

        public int WorldRelationship { get; set; }

        public NetworkClient(Main main) {
            Main = main;

            NetPeerConfiguration config = new NetPeerConfiguration("wh");
            config.AutoFlushSendQueue = false;

            NetClient = new NetClient(config);
            NetClient.Start();
        }

        public NetConnection Connect() {
            NetOutgoingMessage hail = NetClient.CreateMessage("This is the hail message");
            return NetClient.Connect("127.0.0.1", 80, hail);
        }

        public void WritePackets() {
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

            if(Game.Player.Character.CurrentVehicle != null) {
                SendVehiclePacket(Game.Player.Character.CurrentVehicle);
            }
            else {
                SendNoVehiclePacket();
            }
                
            SendPositionPacket();
            SendRotationPacket();

            NetClient.FlushSendQueue();
        }

        private void SendNoVehiclePacket() {
            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage();
            outgoingMessage.Write((byte)PacketType.NoVehicle);
            outgoingMessage.Write(PlayerID);
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.Unreliable);
        }

        private void SendVehiclePacket(Vehicle vehicle) {
            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage();
            outgoingMessage.Write((byte)PacketType.Vehicle);
            outgoingMessage.Write(PlayerID);
            outgoingMessage.Write(vehicle.Model.Hash);
            outgoingMessage.Write(vehicle.Position.X + 10.0f);
            outgoingMessage.Write(vehicle.Position.Y);
            outgoingMessage.Write(vehicle.Position.Z);
            outgoingMessage.Write(vehicle.Quaternion.W);
            outgoingMessage.Write(vehicle.Quaternion.X);
            outgoingMessage.Write(vehicle.Quaternion.Y);
            outgoingMessage.Write(vehicle.Quaternion.Z);
            outgoingMessage.Write((int)vehicle.PrimaryColor);
            outgoingMessage.Write((int)vehicle.SecondaryColor);
            outgoingMessage.Write(vehicle.Speed);
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.Unreliable);
        }

        private void SendAimingPacket(int aimState) {
            Vector3 camPosition = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_COORD);
            Vector3 rot = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 0);
            Vector3 dir = Utility.RotationToDirection(rot);
            Vector3 posLookAt = camPosition + dir * 1000f;

            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage();
            outgoingMessage.Write((byte)PacketType.Aiming);
            outgoingMessage.Write(PlayerID);
            outgoingMessage.Write(aimState);
            outgoingMessage.Write(posLookAt.X);
            outgoingMessage.Write(posLookAt.Y);
            outgoingMessage.Write(posLookAt.Z);
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.Unreliable);
        }

        private void SendPositionPacket() {
            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage();
            outgoingMessage.Write((byte)PacketType.Position);
            outgoingMessage.Write(PlayerID);
            outgoingMessage.Write(Game.Player.Character.Position.X + 3.0f);
            outgoingMessage.Write(Game.Player.Character.Position.Y);
            outgoingMessage.Write(World.GetGroundHeight(Game.Player.Character.Position));
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.Unreliable);
        }

        private void SendRotationPacket() {
            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage();
            outgoingMessage.Write((byte)PacketType.Rotation);
            outgoingMessage.Write(PlayerID);
            outgoingMessage.Write(Game.Player.Character.Rotation.X);
            outgoingMessage.Write(Game.Player.Character.Rotation.Y);
            outgoingMessage.Write(Game.Player.Character.Rotation.Z);
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.Unreliable);
        }

        private void SendShootingPacket(int shootState) {
            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage();
            outgoingMessage.Write((byte)PacketType.Shooting);
            outgoingMessage.Write(PlayerID);
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
            outgoingMessage.Write(PlayerID);
            outgoingMessage.Write(message);
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
        }

        public void ReadPackets() {
            NetIncomingMessage netIncomingMessage;

            while ((netIncomingMessage = NetClient.ServerConnection.Peer.ReadMessage()) != null) {
                if(netIncomingMessage.MessageType == NetIncomingMessageType.StatusChanged) {
                    NetConnectionStatus status = (NetConnectionStatus)netIncomingMessage.ReadByte();

                    if (status == NetConnectionStatus.Connected) {
                        SendRequestInitializationPacket();
                        NetClient.FlushSendQueue();
                    }
                }
                else if(netIncomingMessage.MessageType == NetIncomingMessageType.Data) {
                    PacketType receivedPacket = (PacketType)netIncomingMessage.ReadByte();

                    if (receivedPacket == PacketType.Initialization) {
                        int playerId = netIncomingMessage.ReadInt32();
                        float posX = netIncomingMessage.ReadFloat();
                        float posY = netIncomingMessage.ReadFloat();
                        float posZ = netIncomingMessage.ReadFloat();
                        bool safeForNet = netIncomingMessage.ReadBoolean();

                        // Sets actual playerid
                        PlayerID = playerId;

                        // Changes the physical location
                        Vector3 position = new Vector3(posX, posY, posZ);
                        Game.Player.Character.Position = position;

                        // Sets safe for net flag
                        SafeForNet = safeForNet;

                        Main.ChatBox.Add("(internal) Allocated PlayerID: " + PlayerID);
                        Main.ChatBox.Add("(internal) Packet data: " + receivedPacket + ", " + playerId + ", " + posX + ", " + posY + ", " + posZ + ", " + safeForNet);

                        ServerPlayers = new List<NetworkPlayer>();

                        WorldRelationship = World.AddRelationshipGroup("AMP_PED");
                        World.SetRelationshipBetweenGroups(Relationship.Companion, WorldRelationship, Game.Player.Character.RelationshipGroup); // Make the opposing party ally us (no fleeing)
                        World.SetRelationshipBetweenGroups(Relationship.Neutral, Game.Player.Character.RelationshipGroup, WorldRelationship);

                        // Debug
                        Game.Player.Character.Weapons.Give(WeaponHash.Pistol, 9999, true, true);
                    }
                    else if (receivedPacket == PacketType.Deinitialization) {
                        int playerId = netIncomingMessage.ReadInt32();

                        // ConcurrentModificationException failsafe
                        List<NetworkPlayer> safeServerPlayers = new List<NetworkPlayer>(ServerPlayers);

                        foreach(NetworkPlayer netPlayer in safeServerPlayers) {
                            if(netPlayer.PlayerID == playerId) {
                                netPlayer.Ped.CurrentBlip.Remove();
                                netPlayer.Ped.Delete();

                                ServerPlayers.Remove(netPlayer);
                                Main.ChatBox.Add("(internal) Removed PlayerID: " + playerId);
                            }
                        }
                    }
                    else if (receivedPacket == PacketType.ChatMessage) {
                        string message = netIncomingMessage.ReadString();

                        Main.ChatBox.Add(message);
                    }
                    else if (receivedPacket == PacketType.NoVehicle) {
                        int playerId = netIncomingMessage.ReadInt32();

                        NetworkPlayer networkPlayer = null;
                        foreach (NetworkPlayer serverNetworkPlayer in ServerPlayers) {
                            if (serverNetworkPlayer.PlayerID == playerId)
                                networkPlayer = serverNetworkPlayer;
                        }

                        if(networkPlayer.NetVehicle != null) {
                            Main.ChatBox.Add("Received NoVehicle Packet, NetVehicle != null, removing Vehicle from Entities: " + networkPlayer.PlayerID);

                            networkPlayer.Ped.Task.WarpOutOfVehicle(networkPlayer.NetVehicle.PhysicalVehicle);
                            networkPlayer.NetVehicle = null;
                        }
                    }
                    else if (receivedPacket == PacketType.Vehicle) {
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

                        // Check to see if this player is already in our list
                        NetworkPlayer networkPlayer = null;
                        foreach (NetworkPlayer serverNetworkPlayer in ServerPlayers) {
                            if (serverNetworkPlayer.PlayerID == playerId)
                                networkPlayer = serverNetworkPlayer;
                        }

                        if(networkPlayer.NetVehicle != null) {
                            networkPlayer.NetVehicle.Hash = vehicleHash;
                            networkPlayer.NetVehicle.PosX = posX;
                            networkPlayer.NetVehicle.PosY = posY;
                            networkPlayer.NetVehicle.PosZ = posZ;
                            networkPlayer.NetVehicle.RotW = rotW;
                            networkPlayer.NetVehicle.RotX = rotX;
                            networkPlayer.NetVehicle.RotY = rotY;
                            networkPlayer.NetVehicle.RotZ = rotZ;
                            networkPlayer.NetVehicle.PrimaryColor = primaryColor;
                            networkPlayer.NetVehicle.SecondaryColor = secondaryColor;
                            networkPlayer.NetVehicle.Speed = speed;

                            NetworkVehicle netVehicle = networkPlayer.NetVehicle;

                            var distance = new Vector3(posX, posY, posZ) - netVehicle.PhysicalVehicle.Position;
                            distance.Normalize();

                            netVehicle.PhysicalVehicle.Quaternion = new Quaternion(rotX, rotY, rotZ, rotW);

                            if (speed >= 1 && (Switch % 3 == 0)) {
                                var direction = distance * Math.Abs(speed - netVehicle.PhysicalVehicle.Speed);
                                netVehicle.PhysicalVehicle.ApplyForce(direction);
                            }

                            // Force reset position
                            /*
                            if(!netVehicle.PhysicalVehicle.IsInRangeOf(new Vector3(posX, posY, posZ), 1f)) {
                                netVehicle.PhysicalVehicle.Position = new Vector3(posX, posY, posZ);
                            }
                            */
                        }
                        else {
                            Main.ChatBox.Add("No NetVehicle found.. create one: " + networkPlayer.PlayerID);

                            NetworkVehicle netVehicle = new NetworkVehicle();
                            netVehicle.Hash = vehicleHash;
                            netVehicle.PosX = posX;
                            netVehicle.PosY = posY;
                            netVehicle.PosZ = posZ;
                            netVehicle.RotW = rotW;
                            netVehicle.RotX = rotX;
                            netVehicle.RotY = rotY;
                            netVehicle.RotZ = rotZ;
                            netVehicle.PrimaryColor = primaryColor;
                            netVehicle.SecondaryColor = secondaryColor;

                            netVehicle.PhysicalVehicle = World.CreateVehicle(new Model(netVehicle.Hash), new Vector3(netVehicle.PosX, netVehicle.PosY, netVehicle.PosZ), 0);
                            netVehicle.PhysicalVehicle.IsInvincible = true;
                            netVehicle.PhysicalVehicle.EngineRunning = true;
                            Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, netVehicle.PhysicalVehicle, true, true); // More control for us, less control for Rockstar
                            networkPlayer.NetVehicle = netVehicle;

                            networkPlayer.Ped.Task.WarpIntoVehicle(networkPlayer.NetVehicle.PhysicalVehicle, VehicleSeat.Driver);
                        }
                        //Main.ChatBox.Add("PosX: " + posX + ", PosY: " + posY + ", PosZ: " + posZ);
                    }
                    else if (receivedPacket == PacketType.TotalPlayerData) {
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

                        // Check to see if this player is already in our list
                        NetworkPlayer networkPlayer = null;
                        foreach (NetworkPlayer serverNetworkPlayer in ServerPlayers) {
                            if (serverNetworkPlayer.PlayerID == playerId)
                                networkPlayer = serverNetworkPlayer;
                        }

                        // Seems like this is a new player
                        if (networkPlayer == null) {
                            networkPlayer = new NetworkPlayer();
                            networkPlayer.PlayerID = playerId;
                            networkPlayer.Position = new Vector3(posX, posY, posZ);

                            // Create the physical ped
                            networkPlayer.Ped = World.CreatePed(PedHash.FibArchitect, networkPlayer.Position);
                            networkPlayer.Ped.Weapons.Give(WeaponHash.Pistol, 500, true, true);

                            // DEBUG, not sure how this should be sorted out, reminder clone testing!
                            networkPlayer.Ped.RelationshipGroup = Game.Player.Character.RelationshipGroup;
                            networkPlayer.Ped.BlockPermanentEvents = true;
                            networkPlayer.Ped.CanRagdoll = false;
                            networkPlayer.Ped.IsInvincible = true;
                            networkPlayer.Ped.AddBlip();
                            networkPlayer.Ped.CurrentBlip.Color = BlipColor.White;
                            networkPlayer.Ped.CurrentBlip.Scale = 0.8f;
                            Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, networkPlayer.Ped, true, true); // More control for us, less control for Rockstar
                            networkPlayer.Ped.RelationshipGroup = WorldRelationship;
                            

                            if (PlayerID == networkPlayer.PlayerID) {
                                Main.ChatBox.Add("(internal) - This is also our LocalPlayer");
                                networkPlayer.LocalPlayer = true;
                            }

                            ServerPlayers.Add(networkPlayer);

                            Main.ChatBox.Add("(internal) Added PlayerID " + networkPlayer.PlayerID + " to the ServerPlayers list");
                        }

                        Point namePlate = UI.WorldToScreen(networkPlayer.Ped.Position + new Vector3(0, 0, 1.3f));
                        Main.NametagUI.SetNametagForPlayer(new Nametag { NetworkPlayer = networkPlayer, Point = namePlate });

                        networkPlayer.Aiming = aiming;
                        networkPlayer.AimPosition = new Vector3(aimPosX, aimPosY, aimPosZ);
                        
                        networkPlayer.Shooting = shooting;

                        // Update the player if he's not in a vehicle
                        if(networkPlayer.NetVehicle == null) {
                            if (networkPlayer.Aiming == 1 && networkPlayer.Shooting == 0 && (Switch % 15 == 0)) {
                                if (posX != networkPlayer.Position.X || posY != networkPlayer.Position.Y || posZ != networkPlayer.Position.Z) {
                                    Function.Call(Hash.TASK_GO_TO_COORD_WHILE_AIMING_AT_COORD, networkPlayer.Ped.Handle, networkPlayer.Position.X, networkPlayer.Position.Y, networkPlayer.Position.Z, networkPlayer.AimPosition.X, networkPlayer.AimPosition.Y, networkPlayer.AimPosition.Z, 2f, 0, 0x3F000000, 0x40800000, 1, 512, 0, (uint)FiringPattern.FullAuto);
                                    BlockAimAtTask = true;
                                }
                            }
                            else if (networkPlayer.Aiming == 1 && networkPlayer.Shooting == 0 && !BlockAimAtTask) {
                                networkPlayer.Ped.Task.AimAt(networkPlayer.AimPosition, 200);
                            }

                            if (networkPlayer.Shooting == 1) {
                                //Function.Call(Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS, networkPlayer.Ped.Position.X, networkPlayer.Ped.Position.Y, networkPlayer.Ped.Position.Z, networkPlayer.AimPosition.X, networkPlayer.AimPosition.Y, networkPlayer.AimPosition.Z, 50, true, 0x1B06D571, networkPlayer.Ped, true, false, 100);

                                networkPlayer.Ped.ShootRate = 100000;
                                networkPlayer.Ped.Task.ShootAt(networkPlayer.AimPosition, 325);

                                BlockAimAtTask = true;
                            }

                            if (networkPlayer.Aiming == 0 && networkPlayer.Shooting == 0) {
                                if (posX != networkPlayer.Position.X || posY != networkPlayer.Position.Y || posZ != networkPlayer.Position.Z) {
                                    networkPlayer.Ped.Task.RunTo(networkPlayer.Position, true, 500);
                                }
                            }
                        }

                        networkPlayer.Position = new Vector3(posX, posY, posZ);

                        // Force reset position if we are too desyncy
                        if (!networkPlayer.Ped.IsInRangeOf(networkPlayer.Position, 15f)) {
                            networkPlayer.Ped.Position = networkPlayer.Position;
                        }

                        if (BlockAimAtTask) {
                            BlockAimAtTimer++;
                            if (BlockAimAtTimer == 33) {
                                BlockAimAtTimer = 0;
                                BlockAimAtTask = false;
                            }
                        }
                        Switch++;
                    }
                }
                NetClient.Recycle(netIncomingMessage);
            }
        }

        public void Disconnect() {
            foreach (NetworkPlayer serverNetworkPlayer in ServerPlayers) {
                serverNetworkPlayer.Ped.CurrentBlip.Remove();
                serverNetworkPlayer.Ped.Delete();
            }
            NetClient.Disconnect("bye");
        }

        public NetworkPlayer GetLocalPlayer() {
            foreach(NetworkPlayer networkPlayer in ServerPlayers) {
                if (networkPlayer.LocalPlayer)
                    return networkPlayer;
            }
            return null;
        }
    }
}
