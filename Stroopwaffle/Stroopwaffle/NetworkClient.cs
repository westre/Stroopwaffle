using GTA;
using GTA.Math;
using GTA.Native;
using Lidgren.Network;
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
        private char KeyValueDelimiter { get; } = '@';
        public int PlayerID { get; set; } = -1;
        private bool LocalPlayer { get; set; }
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

        public void Disconnect() {
            NetClient.Disconnect("bye");
        }

        public void SendToServer(string message) {
            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage(message);
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
            NetClient.FlushSendQueue();
        }

        public void SendToServer(string header, string message) {
            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage(header + KeyValueDelimiter + message);
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
            NetClient.FlushSendQueue();
        }

        public void SendToServerUnreliable(string header, string message) {
            NetOutgoingMessage outgoingMessage = NetClient.CreateMessage(header + KeyValueDelimiter + message);
            NetClient.SendMessage(outgoingMessage, NetDeliveryMethod.Unreliable);
            NetClient.FlushSendQueue();
        }

        public NetworkPlayer GetLocalPlayer() {
            foreach(NetworkPlayer networkPlayer in ServerPlayers) {
                if (networkPlayer.LocalPlayer)
                    return networkPlayer;
            }
            return null;
        }

        public void HandleServerMessage(string message) {
            if (message.Contains("server_to_client::send_message")) {
                ProcessMessage(message);
            }

            if (message.Contains("server_to_client::send_player_id")) {
                ProcessPlayerID(message);
            }

            if (message.Contains("server_to_client::update_player_list")) {
                ProcessPlayers(message);         
            }

            if (message.Contains("server_to_client::send_position")) {
                ProcessInitialPosition(message);  
            }

            if (message.Contains("server_to_client::safe_for_net")) {
                ProcessSafeForNet(message);           
            }
        }

        private void ProcessSafeForNet(string message) {
            string[] messageData = message.Split(KeyValueDelimiter); // [0]server_to_client::safe_for_net@[1]1,1
            string stringData = messageData[1]; // 1,1
            string[] playerData = stringData.Split(','); // [0]1 [1]1

            int playerId = int.Parse(playerData[0]);
            int safeForNet = int.Parse(playerData[1]);

            SafeForNet = Convert.ToBoolean(safeForNet);

            Main.ChatBox.Add("(internal) SafeForNet: " + SafeForNet);
        }

        private void ProcessInitialPosition(string message) {
            string[] messageData = message.Split(KeyValueDelimiter); // [0]server_to_client::send_position@[1]pid=1,x=2,y=2,z=3
            string positionData = messageData[1]; // pid=1,x=2,y=2,z=3
            string[] keyValueData = positionData.Split(','); // [0]pid=1 [1]x=2 [2]y=2 [3]z=3

            int playerId = -1;
            float x = -1;
            float y = -1;
            float z = -1;

            foreach (string dataObject in keyValueData) {
                if (dataObject.Contains("pid")) {
                    playerId = int.Parse(dataObject.Split('=')[1]);
                }
                else if (dataObject.Contains("x")) {
                    x = float.Parse(dataObject.Split('=')[1]);
                }
                else if (dataObject.Contains("y")) {
                    y = float.Parse(dataObject.Split('=')[1]);
                }
                else if (dataObject.Contains("z")) {
                    z = float.Parse(dataObject.Split('=')[1]);
                }
            }

            Vector3 position = new Vector3(x, y, z);
            Game.Player.Character.Position = position;
        }

        private void ProcessPlayers(string message) {
            string[] messageData = message.Split(KeyValueDelimiter); // [0]server_to_client::update_player_list@[1]0,2,2,3:1,2,2,3:2,2,2,3
            string playerList = messageData[1]; // 0,2,2,3:1,2,2,3:2,2,2,3
            string[] players = playerList.Split(':'); // [0]0,2,2,3 [1]1,2,2,3 [2]2,2,2,3

            foreach (string player in players) {
                string[] playerData = player.Split('^');

                int playerId = int.Parse(playerData[0]);
                float posX = float.Parse(playerData[1]);
                float posY = float.Parse(playerData[2]);
                float posZ = float.Parse(playerData[3]);
                float rotX = float.Parse(playerData[4]);
                float rotY = float.Parse(playerData[5]);
                float rotZ = float.Parse(playerData[6]);
                int aiming = int.Parse(playerData[7]);
                float aimPosX = float.Parse(playerData[8]);
                float aimPosY = float.Parse(playerData[9]);
                float aimPosZ = float.Parse(playerData[10]);
                int shooting = int.Parse(playerData[11]);

                // Check to see if this player is already in our list
                NetworkPlayer foundNetworkPlayer = null;
                foreach (NetworkPlayer networkPlayer in ServerPlayers) {
                    if (networkPlayer.PlayerID == playerId)
                        foundNetworkPlayer = networkPlayer;
                }

                if (foundNetworkPlayer == null) {
                    // This player does not exist in our list, add the player
                    // This is also our own player!
                    NetworkPlayer newNetworkPlayer = new NetworkPlayer();
                    newNetworkPlayer.PlayerID = playerId;
                    newNetworkPlayer.Position = new Vector3(posX, posY, posZ);
                    newNetworkPlayer.Rotation = new Vector3(rotX, rotY, rotZ);
                    newNetworkPlayer.Aiming = aiming;
                    newNetworkPlayer.AimPosition = new Vector3(aimPosX, aimPosY, aimPosZ);
                    newNetworkPlayer.Shooting = shooting;

                    // Create the damn ped
                    newNetworkPlayer.Ped = World.CreatePed(PedHash.FibArchitect, newNetworkPlayer.Position);

                    // Debug
                    newNetworkPlayer.Ped.Weapons.Give(WeaponHash.Pistol, 500, true, true);

                    ServerPlayers.Add(newNetworkPlayer);

                    Main.ChatBox.Add("(internal) Added PlayerID " + newNetworkPlayer.PlayerID + " to the ServerPlayers list");

                    if (PlayerID == newNetworkPlayer.PlayerID) {
                        Main.ChatBox.Add("(internal) - This is also our LocalPlayer");
                        newNetworkPlayer.LocalPlayer = true;
                    }
                }
                else {
                    // Update the players and their peds!
                    if (foundNetworkPlayer.Ped != null) {
                        foundNetworkPlayer.Position = new Vector3(posX, posY, posZ);
                        foundNetworkPlayer.Ped.Position = foundNetworkPlayer.Position;

                        foundNetworkPlayer.Aiming = aiming;
                        foundNetworkPlayer.AimPosition = new Vector3(aimPosX, aimPosY, aimPosZ);

                        foundNetworkPlayer.Shooting = shooting;

                        // DEBUG, not sure how this should be sorted out, reminder clone testing!
                        foundNetworkPlayer.Ped.RelationshipGroup = Game.Player.Character.RelationshipGroup;

                        if (foundNetworkPlayer.Shooting == 1) {
                            foundNetworkPlayer.Ped.ShootRate = 1000;
                            foundNetworkPlayer.Ped.Task.ShootAt(foundNetworkPlayer.AimPosition, 500);

                            Main.ChatBox.Add("(internal) SHOOT: " + foundNetworkPlayer.PlayerID);
                            //Script.Wait(500); // TODO: do this shit differently
                        }
                        else {
                            if (foundNetworkPlayer.Aiming == 1) {
                                // is the shooting task running?
                                foundNetworkPlayer.Ped.Task.AimAt(foundNetworkPlayer.AimPosition, 1000);
                            }
                            else {
                                foundNetworkPlayer.Rotation = new Vector3(rotX, rotY, rotZ);
                                foundNetworkPlayer.Ped.Rotation = new Vector3(rotX, rotY, rotZ);
                            }
                        }


                        //UI.Notify("PID: " + foundNetworkPlayer.PlayerID + ", Pos: " + foundNetworkPlayer.Position.ToString());
                    }
                    else {
                        UI.Notify("Ped is null, wtf?");
                    }
                }
            }
        }

        private void ProcessMessage(string message) {
            string[] messageData = message.Split(KeyValueDelimiter);
            Main.ChatBox.Add(messageData[1]);
        }

        private void ProcessPlayerID(string message) {
            string[] messageData = message.Split(KeyValueDelimiter);

            PlayerID = int.Parse(messageData[1]);
            Main.ChatBox.Add("(internal) Allocated PlayerID: " + PlayerID);

            LocalPlayer = true;
            ServerPlayers = new List<NetworkPlayer>();

            // Debug
            Game.Player.Character.Weapons.Give(WeaponHash.Pistol, 500, true, true);
        }
    }
}
