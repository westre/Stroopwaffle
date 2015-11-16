using Lidgren.Network;
using NLua;
using Stroopwaffle_Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Stroopwaffle_Server {
    public class Server : NetServer {
        public ServerForm Form { get; set; }
        public List<NetworkPlayer> Players { get; set; }

        private char KeyValueDelimiter { get; } = '@';
        private bool[] PlayerIDs { get; set; }

        public Server(ServerForm form, NetPeerConfiguration config) : base(config) {
            Form = form;

            Start();

            PlayerIDs = new bool[100];

            Players = new List<NetworkPlayer>();

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 10;
            timer.Tick += BroadcastPlayerData;
            timer.Start();

            form.Output("Initialized server");

            // LUA test
            API api = new API(this);

            api.Lua.DoString(@"
            local value = 23

            function testFunction(value)
                broadcastMessage('Yes, the value is : ' .. value)
            end

            testFunction(value)

            function OnScriptInitialize()
                broadcastMessage('Script has been initialized!')
            end

            function OnScriptExit()
                broadcastMessage('Script has been exited.')
            end

            function OnPlayerConnect(playerId)
                broadcastMessage('Player ' .. playerId .. ' has joined the server.')
            end

            function OnPlayerDisconnect(playerId)
                broadcastMessage('Player ' .. playerId .. ' has left the server.')
            end

            function OnPlayerChat(playerId, message)
                broadcastMessage(playerId .. ': ' .. message)
            end
            ");

            api.Fire(API.Callback.OnScriptInitialize);
            api.Fire(API.Callback.OnPlayerConnect, 2);
            api.Fire(API.Callback.OnPlayerDisconnect, 2);
            api.Fire(API.Callback.OnPlayerChat, 2, "Hello!");
            api.Fire(API.Callback.OnScriptExit);
        }

        private void BroadcastPlayerData(object sender, EventArgs e) {
            SendPlayerListPacket();
        }

        public void Application_Idle(object sender, EventArgs e) {
            while (NativeMethods.AppStillIdle) {
                NetIncomingMessage netIncomingMessage;
                while ((netIncomingMessage = ReadMessage()) != null) {
                    switch (netIncomingMessage.MessageType) {
                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.ErrorMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.VerboseDebugMessage:
                            string text = netIncomingMessage.ReadString();
                            Form.Output(text);
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            NetConnectionStatus status = (NetConnectionStatus)netIncomingMessage.ReadByte();

                            string reason = netIncomingMessage.ReadString();
                            Form.Output(NetUtility.ToHexString(netIncomingMessage.SenderConnection.RemoteUniqueIdentifier) + " " + status + ": " + reason);

                            if (status == NetConnectionStatus.Connected) {
                                Form.Output("Remote hail: " + netIncomingMessage.SenderConnection.RemoteHailMessage.ReadString());

                                NetworkPlayer networkPlayer = new NetworkPlayer(netIncomingMessage.SenderConnection);
                                Players.Add(networkPlayer);

                                Form.Output("Added NetworkPlayer to list, size: " + Players.Count);
                            }
                            else if(status == NetConnectionStatus.Disconnected) {
                                NetworkPlayer networkPlayer = Find(netIncomingMessage.SenderConnection);

                                if (networkPlayer != null) {
                                    Players.Remove(networkPlayer);
                                    SendDeinitializationPacket(networkPlayer.PlayerID);
                                    Form.Output("Removed NetworkPlayer from list, size: " + Players.Count);
                                }
                                else {
                                    Form.Output("ERROR: Could not find NetworkPlayer from list");
                                }

                                // Update all players with the new information
                                FreePlayerID(networkPlayer.PlayerID);
                            }
                            break;
                        case NetIncomingMessageType.Data:
                            PacketType receivedPacket = (PacketType)netIncomingMessage.ReadByte();

                            switch (receivedPacket) {                                
                                case PacketType.Initialization:
                                    NetworkPlayer networkPlayer = Find(netIncomingMessage.SenderConnection);
                                    
                                    // Allocate Player ID for the newly connected player
                                    int newPlayerId = FindAvailablePlayerID();
                                    if (newPlayerId != -1) {
                                        if (AllocatePlayerID(newPlayerId)) {
                                            // PlayerID for newly created player
                                            networkPlayer.PlayerID = newPlayerId;                                        

                                            // Initial position data for the newly connected player
                                            networkPlayer.Position = new Vector3(157f, -1649f, 30f);
                                            networkPlayer.Rotation = new Vector3(0f, 0f, 0f);
                                            networkPlayer.AimLocation = new Vector3(0f, 0f, 0f);

                                            // Safe for network interaction
                                            networkPlayer.SafeForNet = true;

                                            // Send the packet to the player
                                            SendInitializationPacket(networkPlayer.NetConnection, networkPlayer.PlayerID, networkPlayer.Position, networkPlayer.SafeForNet);
                                            FlushSendQueue();

                                            Form.Output("Allocated PlayerID " + newPlayerId + ", for: " + networkPlayer.NetConnection.RemoteUniqueIdentifier);
                                        }
                                        else {
                                            Form.Output("Could not allocate player id");
                                        }
                                    }
                                    else {
                                        Form.Output("Could not find an available player id");
                                    }
                                    break;

                                case PacketType.Position:
                                    networkPlayer = Find(netIncomingMessage.SenderConnection);

                                    int playerId = netIncomingMessage.ReadInt32();
                                    float x = netIncomingMessage.ReadFloat();
                                    float y = netIncomingMessage.ReadFloat();
                                    float z = netIncomingMessage.ReadFloat();

                                    if (networkPlayer.PlayerID == playerId) {
                                        networkPlayer.Position = new Vector3(x, y, z);
                                        //Form.Output("Updated Position for ID: "+ playerId + " - " + networkPlayer.Position.ToString());
                                    }
                                    else {
                                        Form.Output("Fatal error: PlayerID mismatch!!!" + " NetworkPlayer: " + networkPlayer.PlayerID + ",id: " + playerId);
                                    }
                                    break;

                                case PacketType.Rotation:
                                    networkPlayer = Find(netIncomingMessage.SenderConnection);

                                    playerId = netIncomingMessage.ReadInt32();
                                    x = netIncomingMessage.ReadFloat();
                                    y = netIncomingMessage.ReadFloat();
                                    z = netIncomingMessage.ReadFloat();

                                    if (networkPlayer.PlayerID == playerId) {
                                        networkPlayer.Rotation = new Vector3(x, y, z);
                                        //Form.Output("Updated Rotation for ID: " + playerId + " - " + networkPlayer.Rotation.ToString());
                                    }
                                    else {
                                        Form.Output("Fatal error: PlayerID mismatch!!!" + " NetworkPlayer: " + networkPlayer.PlayerID + ",id: " + playerId);
                                    }
                                    break;

                                case PacketType.Aiming:
                                    networkPlayer = Find(netIncomingMessage.SenderConnection);

                                    playerId = netIncomingMessage.ReadInt32();
                                    int aimState = netIncomingMessage.ReadInt32();
                                    x = netIncomingMessage.ReadFloat();
                                    y = netIncomingMessage.ReadFloat();
                                    z = netIncomingMessage.ReadFloat();

                                    if (networkPlayer.PlayerID == playerId) {
                                        networkPlayer.Aiming = aimState;
                                        networkPlayer.AimLocation = new Vector3(x, y, z);
                                        //Form.Output("Updated AimState for ID: " + playerId + " - " + networkPlayer.Aiming + "-" + networkPlayer.AimLocation.ToString());
                                    }
                                    else {
                                        Form.Output("Fatal error: PlayerID mismatch!!!" + " NetworkPlayer: " + networkPlayer.PlayerID + ",id: " + playerId);
                                    }
                                    break;

                                case PacketType.Shooting:
                                    networkPlayer = Find(netIncomingMessage.SenderConnection);

                                    playerId = netIncomingMessage.ReadInt32();
                                    int shootState = netIncomingMessage.ReadInt32();

                                    if (networkPlayer.PlayerID == playerId) {
                                        networkPlayer.Shooting = shootState;

                                        if (shootState == 1)
                                            Form.Output("Updated ShootState for ID: " + playerId + " - " + networkPlayer.Shooting);
                                    }
                                    else {
                                        Form.Output("Fatal error: PlayerID mismatch!!!" + " NetworkPlayer: " + networkPlayer.PlayerID + ",id: " + playerId);
                                    }
                                    break;

                                default:
                                    Form.Output("Invalid Packet");
                                    break;
                            }
                            break;
                        default:
                            Form.Output("Unhandled type: " + netIncomingMessage.MessageType + " " + netIncomingMessage.LengthBytes + " bytes " + netIncomingMessage.DeliveryMethod + "|" + netIncomingMessage.SequenceChannel);
                            break;
                    }
                    Recycle(netIncomingMessage);
                }
                Thread.Sleep(1);
            }
        }

        public void SendBroadcastMessagePacket(string message) {
            Form.Output("SBMC: " + message);
        }

        private void SendDeinitializationPacket(int playerID) {
            NetOutgoingMessage outgoingMessage = CreateMessage();
            outgoingMessage.Write((byte)PacketType.Deinitialization);
            outgoingMessage.Write(playerID);
            SendMessage(outgoingMessage, GetAllConnections(), NetDeliveryMethod.ReliableOrdered, 0);
        }

        private void SendInitializationPacket(NetConnection netConnection, int playerID, Vector3 position, bool safeForNet) {
            NetOutgoingMessage outgoingMessage = CreateMessage();
            outgoingMessage.Write((byte)PacketType.Initialization);
            outgoingMessage.Write(playerID);
            outgoingMessage.Write(position.X);
            outgoingMessage.Write(position.Y);
            outgoingMessage.Write(position.Z);
            outgoingMessage.Write(safeForNet);
            SendMessage(outgoingMessage, netConnection, NetDeliveryMethod.ReliableOrdered);
        }

        private void SendPlayerListPacket() {
            if (Players.Count > 0) {
                foreach (NetworkPlayer netPlayer in Players) {
                    if(netPlayer.SafeForNet) {
                        NetOutgoingMessage outgoingMessage = CreateMessage();
                        outgoingMessage.Write((byte)PacketType.TotalPlayerData);
                        outgoingMessage.Write(netPlayer.PlayerID);
                        outgoingMessage.Write(netPlayer.Position.X);
                        outgoingMessage.Write(netPlayer.Position.Y);
                        outgoingMessage.Write(netPlayer.Position.Z);
                        outgoingMessage.Write(netPlayer.Rotation.X);
                        outgoingMessage.Write(netPlayer.Rotation.Y);
                        outgoingMessage.Write(netPlayer.Rotation.Z);
                        outgoingMessage.Write(netPlayer.Aiming);
                        outgoingMessage.Write(netPlayer.AimLocation.X);
                        outgoingMessage.Write(netPlayer.AimLocation.Y);
                        outgoingMessage.Write(netPlayer.AimLocation.Z);
                        outgoingMessage.Write(netPlayer.Shooting);
                        SendMessage(outgoingMessage, GetAllConnections(), NetDeliveryMethod.Unreliable, 0);
                    }
                }   
            }            
        }

        private NetworkPlayer Find(NetConnection netConnection) {
            foreach (NetworkPlayer player in Players) {
                if (player.NetConnection == netConnection)
                    return player;
            }
            return null;
        }

        private List<NetConnection> GetAllConnections() {
            List<NetConnection> connections = new List<NetConnection>();
            foreach(NetworkPlayer player in Players) {
                connections.Add(player.NetConnection);
            }
            return connections;
        }

        private void FreePlayerID(int playerId) {
            PlayerIDs[playerId] = false;
        }

        private int FindAvailablePlayerID() {
            for(int index = 0; index < PlayerIDs.Length; index++) {
                if(!PlayerIDs[index]) {
                    return index;
                }
            }
            return -1;
        }
        
        private bool AllocatePlayerID(int playerId) {
            if(!PlayerIDs[playerId]) {
                PlayerIDs[playerId] = true;
                return true;
            }
            return false;
        } 
    }
}
