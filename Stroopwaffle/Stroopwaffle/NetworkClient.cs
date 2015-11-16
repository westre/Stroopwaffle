using GTA;
using GTA.Math;
using GTA.Native;
using Lidgren.Network;
using Stroopwaffle_Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stroopwaffle {
    class NetworkClient {
        private Main Main { get; set; }
        public NetClient NetClient { get; set; }
        public int PlayerID { get; set; } = -1;
        private List<NetworkPlayer> ServerPlayers { get; set; }
        public bool SafeForNet { get; set; }

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

            SendPositionPacket();
            SendRotationPacket();

            NetClient.FlushSendQueue();
        }

        private void SendAimingPacket(int aimState) {
            Vector3 camPosition = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_COORD);
            Vector3 rot = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 0);
            Vector3 dir = Main.RotationToDirection(rot);
            Vector3 posLookAt = camPosition + dir * 10f;

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

        private void SendRequestionInitializationPacket() {
            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage();
            outgoingMessage.Write((byte)PacketType.Initialization);
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
        }

        public void ReadPackets() {
            NetIncomingMessage netIncomingMessage;

            while ((netIncomingMessage = NetClient.ServerConnection.Peer.ReadMessage()) != null) {
                if(netIncomingMessage.MessageType == NetIncomingMessageType.StatusChanged) {
                    NetConnectionStatus status = (NetConnectionStatus)netIncomingMessage.ReadByte();

                    if (status == NetConnectionStatus.Connected) {
                        SendRequestionInitializationPacket();
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

                        // Debug
                        Game.Player.Character.Weapons.Give(WeaponHash.Pistol, 500, true, true);
                    }
                    else if (receivedPacket == PacketType.Deinitialization) {
                        int playerId = netIncomingMessage.ReadInt32();

                        // ConcurrentModificationException failsafe
                        List<NetworkPlayer> safeServerPlayers = new List<NetworkPlayer>(ServerPlayers);

                        foreach(NetworkPlayer netPlayer in safeServerPlayers) {
                            if(netPlayer.PlayerID == playerId) {
                                ServerPlayers.Remove(netPlayer);
                                Main.ChatBox.Add("(internal) Removed PlayerID: " + playerId);
                            }
                        }
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

                            if (PlayerID == networkPlayer.PlayerID) {
                                Main.ChatBox.Add("(internal) - This is also our LocalPlayer");
                                networkPlayer.LocalPlayer = true;
                            }

                            ServerPlayers.Add(networkPlayer);

                            Main.ChatBox.Add("(internal) Added PlayerID " + networkPlayer.PlayerID + " to the ServerPlayers list");
                        }

                        // Update the player
                        networkPlayer.Position = new Vector3(posX, posY, posZ);
                        networkPlayer.Ped.Position = networkPlayer.Position;

                        networkPlayer.Aiming = aiming;
                        networkPlayer.AimPosition = new Vector3(aimPosX, aimPosY, aimPosZ);

                        networkPlayer.Shooting = shooting;

                        // DEBUG, not sure how this should be sorted out, reminder clone testing!
                        networkPlayer.Ped.RelationshipGroup = Game.Player.Character.RelationshipGroup;

                        if (networkPlayer.Shooting == 1) {
                            networkPlayer.Ped.ShootRate = 1000;
                            networkPlayer.Ped.Task.ShootAt(networkPlayer.AimPosition, 500);

                            Main.ChatBox.Add("(internal) SHOOT: " + networkPlayer.PlayerID);
                            //Script.Wait(500); // TODO: do this shit differently
                        }
                        else {
                            if (networkPlayer.Aiming == 1) {
                                // is the shooting task running?
                                networkPlayer.Ped.Task.AimAt(networkPlayer.AimPosition, 1000);
                            }
                            else {
                                networkPlayer.Rotation = new Vector3(rotX, rotY, rotZ);
                                networkPlayer.Ped.Rotation = new Vector3(rotX, rotY, rotZ);
                            }
                        }
                    }
                }
                NetClient.Recycle(netIncomingMessage);
            }
        }

        public void Disconnect() {
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
