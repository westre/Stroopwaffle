﻿using Lidgren.Network;
using NLua;
using Stroopwaffle_Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Net;

namespace Stroopwaffle_Server {
    public class Server : NetServer {
        public ServerForm Form { get; set; }

        public List<NetworkPlayer> Players { get; set; }
        public List<NetworkVehicle> Vehicles { get; set; }
        public List<NetworkUI> UIs { get; set; }

        private bool[] PlayerIDs { get; set; }
        private bool[] VehicleIDs { get; set; }
        private bool[] UIIDs { get; set; }

        private API API { get; set; }
        private OOSListener OOSListener { get; set; }

        private float MasterServerLastRegistered { get; set; } = -60.0f;

        public struct ConfigurationData {
            public string ServerName;
            public int Port;
            public string Script;
            public int MaxPlayers;
        }
        public ConfigurationData ConfigData;

        private void LoadConfigurationFile(out ConfigurationData configData) {
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\config.ini")) {
                Form.Output("Configuration file not found, creating a new one...");
                FileStream file = File.Create(AppDomain.CurrentDomain.BaseDirectory + @"\config.ini");
                if (file != null) {
                    Form.Output("Configuration file successfully created, adding initialization data...");

                    file.Close();

                    StreamWriter streamWriter = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + @"\config.ini");

                    string data = "Server Name = Basic server\nPort = 9999\nScript = basic-script.lua\nMaximum Players = 100";
                    streamWriter.WriteLine(data);
                    streamWriter.Close();
                    Form.Output("Initialization data successfully written.");
                }
                else {
                    Form.Output("Could not create configuration file.");
                }
            }

            // https://www.symbiosis-software.com/bibliotek/An-INI-File-Parser-in-C%23
            string[] syntax = {
                @"Server Name = ^..*$",
                @"Port = ^..*$",
                @"Script = ^..*$",
                @"Maximum Players = ^..*$",
            };

            string[] lines = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + @"\config.ini");
            Dictionary<string, string> config = Parser.Parse(lines, false, syntax);

            configData.ServerName = config["Server Name"];
            configData.Port = int.Parse(config["Port"]);
            configData.Script = config["Script"];
            configData.MaxPlayers = int.Parse(config["Maximum Players"]);
        }

        public Server(ServerForm form, NetPeerConfiguration config) : base(config) {
            Form = form;
            List<string> errors = new List<string>();

            API = new API(this);
            OOSListener = new OOSListener(API);

            // Allocate 100 potential player ids
            PlayerIDs = new bool[100];
            // Create list for holding our players
            Players = new List<NetworkPlayer>();
            // Create list to hold our vehicles
            Vehicles = new List<NetworkVehicle>();
            // Allocate 100 potential vehicle ids
            VehicleIDs = new bool[100];
            // Allocate 255 potential networkui ids
            UIIDs = new bool[255];
            // Create list
            UIs = new List<NetworkUI>();

            // Load configuration file
            try {
                LoadConfigurationFile(out ConfigData);

                Form.Text = ConfigData.ServerName;

                if (ConfigData.ServerName.Length > 64)
                    throw new Exception("Server name may only contain a maximum of 64 characters.");

                if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\scripts\" + ConfigData.Script))
                    throw new Exception("Script (" + ConfigData.Script + ") not found.");

                API.Load(AppDomain.CurrentDomain.BaseDirectory + @"\scripts\" + ConfigData.Script);

                API.Fire(API.Callback.OnScriptInitialize);
            }
            catch(Exception ex) {
                errors.Add(ex.Message);
            }
            
            if(errors.Count == 0) {
                // Start the network connection
                Start();

                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                timer.Interval = (int)Rate.ServerTick;
                timer.Tick += Tick;
                timer.Start();

                form.Output("Initialized server");
            }
            else {
                form.Output("----ERRORS----");
                foreach(string error in errors) {
                    form.Output(error);
                }
                form.Output("----END OF ERRORS----");

                API.Fire(API.Callback.OnScriptExit);
            }
        }
        private void Tick(object sender, EventArgs e) {
            SendUpdate();
            API.Tick();
        }

        public void Application_Idle(object sender, EventArgs e) {
            while (NativeMethods.AppStillIdle) {
                // (re-)register periodically with master server
                if (NetTime.Now > MasterServerLastRegistered + 60) {
                    // register with master server
                    NetOutgoingMessage regMsg = CreateMessage();
                    regMsg.Write((byte)MasterServerMessageType.RegisterHost);

                    IPAddress mask;
                    IPAddress adr = NetUtility.GetMyAddress(out mask);

                    regMsg.Write(UniqueIdentifier);
                    regMsg.Write(new IPEndPoint(adr, ConfigData.Port));

                    regMsg.Write(ConfigData.ServerName);
                    regMsg.Write(ConfigData.Script);
                    regMsg.Write(Players.Count);
                    regMsg.Write(ConfigData.MaxPlayers);

                    Form.Output("Sending registration to master server");
                    SendUnconnectedMessage(regMsg, SharedConfiguration.MasterServerEndPoint);
                    MasterServerLastRegistered = (float)NetTime.Now;
                }

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
                                    API.Fire(API.Callback.OnPlayerDisconnect, networkPlayer.PlayerID);
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
                                    string files = netIncomingMessage.ReadString();
                                    
                                    // Allocate Player ID for the newly connected player
                                    int newPlayerId = FindAvailablePlayerID();
                                    if (newPlayerId != -1) {
                                        // PlayerID for newly created player
                                        networkPlayer.PlayerID = newPlayerId;                                        

                                        // Initial position data for the newly connected player
                                        networkPlayer.Position = new Vector3(157f, -1649f, 30f);
                                        networkPlayer.Rotation = new Vector3(0f, 0f, 0f);
                                        networkPlayer.AimLocation = new Vector3(0f, 0f, 0f);
                                        networkPlayer.RunTo = new Vector3(0f, 0f, 0f);
                                        networkPlayer.Model = 3183167778; //Brad

                                        // Safe for network interaction
                                        networkPlayer.SafeForNet = true;

                                        // Send the packet to the player
                                        SendInitializationPacket(networkPlayer.NetConnection, networkPlayer.PlayerID, networkPlayer.Position, networkPlayer.SafeForNet);
                                        FlushSendQueue();

                                        Form.Output("Allocated PlayerID " + newPlayerId + ", for: " + networkPlayer.NetConnection.RemoteUniqueIdentifier);                                    

                                        string[] fileArray = files.Split(',');
                                        Form.Output("File count: " + fileArray.Length);

                                        API.Lua.DoString("filesTable = {}");

                                        foreach(string file in fileArray) {
                                            API.Lua.DoString("table.insert(filesTable, \"" + file + "\")");
                                        }
                                            
                                        LuaTable tab = API.Lua.GetTable("filesTable");
                                        API.Fire(API.Callback.OnPlayerConnect, newPlayerId, tab);

                                        API.Fire(API.Callback.OnPlayerInitialize, newPlayerId);
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
                                    bool walking = netIncomingMessage.ReadBoolean();
                                    bool running = netIncomingMessage.ReadBoolean();
                                    bool sprinting = netIncomingMessage.ReadBoolean();
                                    float runToX = netIncomingMessage.ReadFloat();
                                    float runToY = netIncomingMessage.ReadFloat();
                                    float runToZ = netIncomingMessage.ReadFloat();
                                    bool jumping = netIncomingMessage.ReadBoolean();
                                    bool ragdoll = netIncomingMessage.ReadBoolean();
                                    bool reloading = netIncomingMessage.ReadBoolean();
                                    int health = netIncomingMessage.ReadInt32();
                                    int maxHealth = netIncomingMessage.ReadInt32();
                                    int armor = netIncomingMessage.ReadInt32();
                                    bool dead = netIncomingMessage.ReadBoolean();

                                    if (networkPlayer.PlayerID == playerId) {
                                        // We just died
                                        if (dead && !networkPlayer.Dead) {
                                            API.Fire(API.Callback.OnPlayerDied, playerId);
                                        }
                                        // We have been respawned!
                                        else if (!dead && networkPlayer.Dead) {
                                            SendRevivePedPacket(playerId);
                                            API.Fire(API.Callback.OnPlayerRespawn, playerId);
                                        }

                                        // Update values
                                        networkPlayer.Position = new Vector3(x, y, z);
                                        networkPlayer.Walking = walking;
                                        networkPlayer.Running = running;
                                        networkPlayer.Sprinting = sprinting;
                                        networkPlayer.RunTo = new Vector3(runToX, runToY, runToZ);
                                        networkPlayer.Jumping = jumping;
                                        networkPlayer.Ragdoll = ragdoll;
                                        networkPlayer.Reloading = reloading;
                                        networkPlayer.Health = health;
                                        networkPlayer.MaxHealth = maxHealth;
                                        networkPlayer.Armor = armor;
                                        networkPlayer.Dead = dead;
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

                                case PacketType.OOSPacket:
                                    networkPlayer = Find(netIncomingMessage.SenderConnection);
                                    OOSPacket oosPacket = (OOSPacket)netIncomingMessage.ReadByte();
                                    playerId = netIncomingMessage.ReadInt32();

                                    if(networkPlayer.PlayerID == playerId) {
                                        OOSListener.Process(networkPlayer, oosPacket);
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

                                case PacketType.NoVehicle:
                                    networkPlayer = Find(netIncomingMessage.SenderConnection);

                                    playerId = netIncomingMessage.ReadInt32();

                                    if(networkPlayer.NetVehicle != null) {
                                        NetworkVehicle sourceVehicle = NetworkVehicle.Exists(Vehicles, networkPlayer.NetVehicle.ID);
                                        sourceVehicle.PlayerID = -1;

                                        API.Fire(API.Callback.OnPlayerExitVehicle, playerId, sourceVehicle.ID);

                                        SendNoVehiclePacket(networkPlayer);
                                        networkPlayer.NetVehicle = null;
                                    }
                             
                                    break;

                                case PacketType.CurrentWeapon:
                                    networkPlayer = Find(netIncomingMessage.SenderConnection);

                                    playerId = netIncomingMessage.ReadInt32();
                                    int weaponHash = netIncomingMessage.ReadInt32();

                                    // Unarmed
                                    if(networkPlayer.Weapons.Contains(weaponHash) || (uint)weaponHash == 2725352035) {
                                        networkPlayer.CurrentWeapon = weaponHash;
                                    }
                                    else {
                                        OOSListener.Process(networkPlayer, OOSPacket.InvalidWeapon);
                                    }                                 
                                    break;

                                case PacketType.TotalVehicleData:
                                    networkPlayer = Find(netIncomingMessage.SenderConnection);

                                    int vehicleId = netIncomingMessage.ReadInt32();
                                    playerId = netIncomingMessage.ReadInt32();
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

                                    NetworkVehicle networkVehicle = NetworkVehicle.Exists(Vehicles, vehicleId);
                                    if(networkVehicle != null) {
                                        // Someone just entered this vehicle
                                        if (networkVehicle.PlayerID == -1 && playerId >= 0) {
                                            API.Fire(API.Callback.OnPlayerEnterVehicle, playerId, vehicleId);
                                        }

                                        networkVehicle.PlayerID = playerId;
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
                                        networkVehicle.Seat = seat;

                                        networkPlayer.NetVehicle = networkVehicle;
                                        //Form.Output("Updated vehicleId: " + vehicleId + ", posX: " + posX);
                                    }
                                    else {
                                        Form.Output("ERROR: NON REGISTERED VEHICLE FOUND?!");
                                    }
                                    break;

                                case PacketType.ChatMessage:
                                    networkPlayer = Find(netIncomingMessage.SenderConnection);

                                    playerId = netIncomingMessage.ReadInt32();
                                    string message = netIncomingMessage.ReadString();

                                    if (networkPlayer.PlayerID == playerId) {
                                        API.Fire(API.Callback.OnPlayerChat, playerId, message);
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

        public void SendGUIRemovePacket(NetConnection netConnection, int playerId, int networkUiId) {
            NetOutgoingMessage outgoingMessage = CreateMessage();
            outgoingMessage.Write((byte)PacketType.GUIPacket);
            outgoingMessage.Write((byte)GUIPacket.RemoveGUIElement);
            outgoingMessage.Write(playerId);
            outgoingMessage.Write(networkUiId);
            SendMessage(outgoingMessage, netConnection, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendSetPlayerModelPacket(int playerId, uint model) {
            if (GetAllConnections().Count == 0) return;

            NetOutgoingMessage outgoingMessage = CreateMessage();
            outgoingMessage.Write((byte)PacketType.SetPlayerModel);
            outgoingMessage.Write(playerId);
            outgoingMessage.Write(model);
            SendMessage(outgoingMessage, GetAllConnections(), NetDeliveryMethod.ReliableOrdered, 0);
        }

        public void SendGivePlayerWeaponPacket(int playerId, int weaponId) {
            if (GetAllConnections().Count == 0) return;

            NetOutgoingMessage outgoingMessage = CreateMessage();
            outgoingMessage.Write((byte)PacketType.GivePlayerWeapon);
            outgoingMessage.Write(playerId);
            outgoingMessage.Write(weaponId);
            SendMessage(outgoingMessage, GetAllConnections(), NetDeliveryMethod.ReliableOrdered, 0);
        }

        public void SendNoVehiclePacket(NetworkPlayer source) {
            if (GetAllConnections().Count == 0) return;

            NetOutgoingMessage outgoingMessage = CreateMessage();
            outgoingMessage.Write((byte)PacketType.NoVehicle);
            outgoingMessage.Write(source.PlayerID);
            SendMessage(outgoingMessage, GetAllConnections(), NetDeliveryMethod.Unreliable, 0);
        }

        public void SendBroadcastMessagePacket(string message) {
            if (GetAllConnections().Count == 0) return;

            NetOutgoingMessage outgoingMessage = CreateMessage();
            outgoingMessage.Write((byte)PacketType.ChatMessage);
            outgoingMessage.Write(message);
            SendMessage(outgoingMessage, GetAllConnections(), NetDeliveryMethod.ReliableOrdered, 0);

            Form.Output("SendBroadcastMessagePacket: " + message);
        }

        public void SendBroadcastMessagePacket(NetConnection netConnection, string message) {
            // Just for the sake of redundancy, don't include the playerId so we can keep using PacketType.ChatMessage on the client
            NetOutgoingMessage outgoingMessage = CreateMessage();
            outgoingMessage.Write((byte)PacketType.ChatMessage);
            outgoingMessage.Write(message);
            SendMessage(outgoingMessage, netConnection, NetDeliveryMethod.ReliableOrdered);

            Form.Output("SendBroadcastMessagePacket (client): " + message);
        }

        public void SendSetPlayerPositionPacket(NetConnection netConnection, int playerId, Vector3 position) {
            NetOutgoingMessage outgoingMessage = CreateMessage();
            outgoingMessage.Write((byte)PacketType.SetPedPosition);
            outgoingMessage.Write(playerId);
            outgoingMessage.Write(position.X);
            outgoingMessage.Write(position.Y);
            outgoingMessage.Write(position.Z);
            SendMessage(outgoingMessage, netConnection, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendSetPlayerArmorPacket(int playerId, int armor) {
            if (GetAllConnections().Count == 0) return;

            NetOutgoingMessage outgoingMessage = CreateMessage();
            outgoingMessage.Write((byte)PacketType.SetPlayerArmor);
            outgoingMessage.Write(playerId);
            outgoingMessage.Write(armor);
            SendMessage(outgoingMessage, GetAllConnections(), NetDeliveryMethod.ReliableOrdered, 0);
        }

        public void SendSetPlayerHealthPacket(int playerId, int health) {
            if (GetAllConnections().Count == 0) return;

            NetOutgoingMessage outgoingMessage = CreateMessage();
            outgoingMessage.Write((byte)PacketType.SetPlayerHealth);
            outgoingMessage.Write(playerId);
            outgoingMessage.Write(health);
            SendMessage(outgoingMessage, GetAllConnections(), NetDeliveryMethod.ReliableOrdered, 0);
        }

        public void SendRevivePedPacket(int playerId) {
            if (GetAllConnections().Count == 0) return;

            NetOutgoingMessage outgoingMessage = CreateMessage();
            outgoingMessage.Write((byte)PacketType.NewPed);
            outgoingMessage.Write(playerId);
            SendMessage(outgoingMessage, GetAllConnections(), NetDeliveryMethod.ReliableOrdered, 0);
        }

        private void SendDeinitializationPacket(int playerID) {
            if (GetAllConnections().Count == 0) return;

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

        private void SendUpdate() {
            if (Players.Count > 0) {
                List<NetworkPlayer> safeNetworkPlayerList = new List<NetworkPlayer>(Players);
                foreach (NetworkPlayer netPlayer in safeNetworkPlayerList) {
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
                        outgoingMessage.Write(netPlayer.Walking);
                        outgoingMessage.Write(netPlayer.Running);
                        outgoingMessage.Write(netPlayer.Sprinting);
                        outgoingMessage.Write(netPlayer.RunTo.X);
                        outgoingMessage.Write(netPlayer.RunTo.Y);
                        outgoingMessage.Write(netPlayer.RunTo.Z);
                        outgoingMessage.Write(netPlayer.CurrentWeapon);
                        outgoingMessage.Write(netPlayer.Jumping);
                        outgoingMessage.Write(netPlayer.Visible);
                        outgoingMessage.Write(netPlayer.Frozen);
                        outgoingMessage.Write(netPlayer.Ragdoll);
                        outgoingMessage.Write(netPlayer.Reloading);
                        outgoingMessage.Write(netPlayer.Health);
                        outgoingMessage.Write(netPlayer.MaxHealth);
                        outgoingMessage.Write(netPlayer.Armor);
                        outgoingMessage.Write(netPlayer.Dead);
                        outgoingMessage.Write(netPlayer.Model);
                        SendMessage(outgoingMessage, GetAllConnections(), NetDeliveryMethod.Unreliable, 0);
                    }
                }

                List<NetworkVehicle> safeNetworkVehicleList = new List<NetworkVehicle>(Vehicles);
                foreach(NetworkVehicle networkVehicle in safeNetworkVehicleList) {
                    NetOutgoingMessage outgoingMessage = CreateMessage();
                    outgoingMessage = CreateMessage();
                    outgoingMessage.Write((byte)PacketType.TotalVehicleData);
                    outgoingMessage.Write(networkVehicle.ID);
                    outgoingMessage.Write(networkVehicle.PlayerID);
                    outgoingMessage.Write(networkVehicle.Hash);
                    outgoingMessage.Write(networkVehicle.PosX);
                    outgoingMessage.Write(networkVehicle.PosY);
                    outgoingMessage.Write(networkVehicle.PosZ);
                    outgoingMessage.Write(networkVehicle.RotW);
                    outgoingMessage.Write(networkVehicle.RotX);
                    outgoingMessage.Write(networkVehicle.RotY);
                    outgoingMessage.Write(networkVehicle.RotZ);
                    outgoingMessage.Write(networkVehicle.PrimaryColor);
                    outgoingMessage.Write(networkVehicle.SecondaryColor);
                    outgoingMessage.Write(networkVehicle.Speed);
                    outgoingMessage.Write(networkVehicle.Seat);
                    SendMessage(outgoingMessage, GetAllConnections(), NetDeliveryMethod.Unreliable, 0);
                }

                #warning Change this from BitStream to RPC
                List<NetworkUI> safeNetworkUIList = new List<NetworkUI>(UIs);
                foreach (NetworkUI networkUI in safeNetworkUIList) {
                    NetConnection netConnection = Find(networkUI.PlayerId); // Some optimization, only send it to the player

                    NetOutgoingMessage outgoingMessage = CreateMessage();
                    outgoingMessage = CreateMessage();
                    outgoingMessage.Write((byte)PacketType.GUIPacket);
                    outgoingMessage.Write((byte)networkUI.Type);
                    outgoingMessage.Write(networkUI.ID);
                    outgoingMessage.Write(networkUI.PlayerId);
                    outgoingMessage.Write(networkUI.PosX);
                    outgoingMessage.Write(networkUI.PosY);
                    outgoingMessage.Write(networkUI.SizeX);
                    outgoingMessage.Write(networkUI.SizeY);
                    outgoingMessage.Write(networkUI.R);
                    outgoingMessage.Write(networkUI.G);
                    outgoingMessage.Write(networkUI.B);
                    outgoingMessage.Write(networkUI.A);

                    SendMessage(outgoingMessage, netConnection, NetDeliveryMethod.Unreliable, 0);
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

        public NetConnection Find(int playerId) {
            foreach(NetworkPlayer player in Players) {
                if (player.PlayerID == playerId)
                    return player.NetConnection;
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
                    PlayerIDs[index] = true;
                    return index;
                }
            }
            return -1;
        }

        private int FindAvailableVehicleID() {
            for (int index = 0; index < VehicleIDs.Length; index++) {
                if (!VehicleIDs[index]) {
                    VehicleIDs[index] = true;
                    return index;
                }
            }
            return -1;
        }

        private int FindAvailableNetworkUIID() {
            for (int index = 0; index < UIIDs.Length; index++) {
                if (!UIIDs[index]) {
                    UIIDs[index] = true;
                    return index;
                }
            }
            return -1;
        }

        public void RegisterVehicle(NetworkVehicle networkVehicle) {
            int vehicleId = FindAvailableVehicleID();

            networkVehicle.ID = vehicleId;
            networkVehicle.PlayerID = -1;
            Form.Output("Allocated vehicle id: " + vehicleId);

            Vehicles.Add(networkVehicle);
        }

        public int RegisterNetworkUI(NetworkUI networkUI) {
            int networkUIId = FindAvailableNetworkUIID();

            networkUI.ID = networkUIId;
            Form.Output("Allocated ui id: " + networkUIId);

            UIs.Add(networkUI);

            return networkUIId;
        }
    }
}
