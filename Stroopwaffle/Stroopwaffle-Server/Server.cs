using Lidgren.Network;
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
        }

        private void BroadcastPlayerData(object sender, EventArgs e) {
            BroadcastUpdatePlayerList();
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
                                if(networkPlayer != null) {
                                    Players.Remove(networkPlayer);
                                    Form.Output("Removed NetworkPlayer from list, size: " + Players.Count);
                                }
                                else {
                                    Form.Output("ERROR: Could not find NetworkPlayer from list");
                                }

                                // Update all players with the new information
                                FreePlayerID(networkPlayer.PlayerID);
                              
                                BroadcastUpdatePlayerList();
                            }
                            break;
                        case NetIncomingMessageType.Data:
                            string message = netIncomingMessage.ReadString();

                            // Received request from client to send the client the message of the day
                            if (message.Contains("client_to_server::request_motd")) {
                                NetworkPlayer networkPlayer = Find(netIncomingMessage.SenderConnection);

                                SendToPlayer(networkPlayer.NetConnection, "server_to_client::send_message", "Welcome to my server!");
                            }

                            if (message.Contains("client_to_server::send_my_position")) {
                                NetworkPlayer networkPlayer = Find(netIncomingMessage.SenderConnection);

                                string[] messageData = message.Split(KeyValueDelimiter); // [0]server_to_client::send_message@[1]1,2,2,3
                                string[] playerData = messageData[1].Split('^');

                                int playerId = int.Parse(playerData[0]);
                                float x = float.Parse(playerData[1]);
                                float y = float.Parse(playerData[2]);
                                float z = float.Parse(playerData[3]);

                                if(networkPlayer.PlayerID == playerId) {
                                    networkPlayer.Position = new Vector3(x, y, z);
                                    //Form.Output("Updated Position for ID: "+ playerId + " - " + networkPlayer.Position.ToString());
                                }
                                else {
                                    Form.Output("Fatal error: PlayerID mismatch!!!" + " NetworkPlayer: " + networkPlayer.PlayerID + ",id: " + playerId);
                                }
                            }

                            if (message.Contains("client_to_server::send_my_rotation")) {
                                NetworkPlayer networkPlayer = Find(netIncomingMessage.SenderConnection);

                                string[] messageData = message.Split(KeyValueDelimiter); // [0]server_to_client::send_message@[1]1,2,2,3
                                string[] playerData = messageData[1].Split('^');

                                int playerId = int.Parse(playerData[0]);
                                float x = float.Parse(playerData[1]);
                                float y = float.Parse(playerData[2]);
                                float z = float.Parse(playerData[3]);

                                if (networkPlayer.PlayerID == playerId) {
                                    networkPlayer.Rotation = new Vector3(x, y, z);
                                    //Form.Output("Updated Rotation for ID: " + playerId + " - " + networkPlayer.Rotation.ToString());
                                }
                                else {
                                    Form.Output("Fatal error: PlayerID mismatch!!!" + " NetworkPlayer: " + networkPlayer.PlayerID + ",id: " + playerId);
                                }
                            }
                            
                            if (message.Contains("client_to_server::my_aim_state")) {
                                NetworkPlayer networkPlayer = Find(netIncomingMessage.SenderConnection);

                                string[] messageData = message.Split(KeyValueDelimiter);
                                string[] playerData = messageData[1].Split('^');

                                int playerId = int.Parse(playerData[0]);
                                int aimState = int.Parse(playerData[1]);
                                float x = float.Parse(playerData[2]);
                                float y = float.Parse(playerData[3]);
                                float z = float.Parse(playerData[4]);

                                if (networkPlayer.PlayerID == playerId) {
                                    networkPlayer.Aiming = aimState;
                                    networkPlayer.AimLocation = new Vector3(x, y, z);
                                    //Form.Output("Updated AimState for ID: " + playerId + " - " + networkPlayer.Aiming + "-" + networkPlayer.AimLocation.ToString());
                                }
                                else {
                                    Form.Output("Fatal error: PlayerID mismatch!!!" + " NetworkPlayer: " + networkPlayer.PlayerID + ",id: " + playerId);
                                }
                            }

                            if (message.Contains("client_to_server::my_shoot_state")) {
                                NetworkPlayer networkPlayer = Find(netIncomingMessage.SenderConnection);

                                string[] messageData = message.Split(KeyValueDelimiter);
                                string[] playerData = messageData[1].Split('^');

                                int playerId = int.Parse(playerData[0]);
                                int shootState = int.Parse(playerData[1]);

                                if (networkPlayer.PlayerID == playerId) {
                                    networkPlayer.Shooting = shootState;

                                    if(shootState == 1)
                                        Form.Output("Updated ShootState for ID: " + playerId + " - " + networkPlayer.Shooting);
                                }
                                else {
                                    Form.Output("Fatal error: PlayerID mismatch!!!" + " NetworkPlayer: " + networkPlayer.PlayerID + ",id: " + playerId);
                                }
                            }

                            if (message.Contains("client_to_server::request_player_id")) {
                                NetworkPlayer networkPlayer = Find(netIncomingMessage.SenderConnection);

                                // Allocate Player ID for the newly connected player
                                int newPlayerId = FindAvailablePlayerID();
                                if(newPlayerId != -1) {
                                    if(AllocatePlayerID(newPlayerId)) {
                                        networkPlayer.PlayerID = newPlayerId;
                                        Form.Output("Allocated PlayerID " + newPlayerId + ", for: " + networkPlayer.NetConnection.RemoteUniqueIdentifier);

                                        SendToPlayer(networkPlayer.NetConnection, "server_to_client::send_player_id", networkPlayer.PlayerID.ToString());
                                    }
                                    else {
                                        Form.Output("Could not allocate player id");
                                    }
                                }
                                else {
                                    Form.Output("Could not find an available player id");
                                }

                                // Initial position data for the newly connected player
                                networkPlayer.Position = new Vector3(157f, -1649f, 30f);
                                networkPlayer.Rotation = new Vector3(0f, 0f, 0f);
                                networkPlayer.AimLocation = new Vector3(0f, 0f, 0f);
                                SendToPlayer(networkPlayer.NetConnection, "server_to_client::send_position", "pid= " + networkPlayer.PlayerID + ",x=" + networkPlayer.Position.X + ",y=" + networkPlayer.Position.Y + ",z=" + networkPlayer.Position.Z);

                                // Safe for network interaction
                                networkPlayer.SafeForNet = true;
                                // Tell the server he is safe for net transmissions
                                SendToPlayer(networkPlayer.NetConnection, "server_to_client::safe_for_net", networkPlayer.PlayerID + ",1");                               

                                // Let's assume the new player has been initialized, update all connected players about all the players
                                BroadcastUpdatePlayerList();
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

        // send to all players
        private void Broadcast(string header, string message) {
            NetOutgoingMessage outgoingMessage = CreateMessage();
            outgoingMessage.Write(header + KeyValueDelimiter + message);
            SendMessage(outgoingMessage, GetAllConnections(), NetDeliveryMethod.ReliableOrdered, 0);
        }

        private void BroadcastUnreliable(string header, string message) {
            NetOutgoingMessage outgoingMessage = CreateMessage();
            outgoingMessage.Write(header + KeyValueDelimiter + message);
            SendMessage(outgoingMessage, GetAllConnections(), NetDeliveryMethod.Unreliable, 0);
        }

        // send to one player
        private void SendToPlayer(NetConnection player, string header, string message) {
            NetOutgoingMessage outgoingMessage = CreateMessage();
            outgoingMessage.Write(header + KeyValueDelimiter + message);
            SendMessage(outgoingMessage, player, NetDeliveryMethod.ReliableOrdered, 0);

            Form.Output("SendToPlayer: " + header + KeyValueDelimiter + message);
        }

        private void BroadcastUpdatePlayerList() {
            if (Players.Count > 0) {
                StringBuilder stringBuilder = new StringBuilder();

                foreach (NetworkPlayer netPlayer in Players) {
                    if(netPlayer.SafeForNet) {
                        stringBuilder.Append(
                            netPlayer.PlayerID + "^" 
                            + netPlayer.Position.X + "^" 
                            + netPlayer.Position.Y + "^"
                            + netPlayer.Position.Z + "^" 
                            + netPlayer.Rotation.X + "^" 
                            + netPlayer.Rotation.Y + "^" 
                            + netPlayer.Rotation.Z + "^" 
                            + netPlayer.Aiming + "^"
                            + netPlayer.AimLocation.X + "^"
                            + netPlayer.AimLocation.Y + "^"
                            + netPlayer.AimLocation.Z + "^"
                            + netPlayer.Shooting + ":"
                        );
                    }
                }

                if(stringBuilder.Length > 0) {
                    stringBuilder.Length--;

                    Broadcast("server_to_client::update_player_list", stringBuilder.ToString());
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
