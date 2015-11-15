using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.Math;
using Stroopwaffle;
using Lidgren.Network;

/*
    http://www.dev-c.com/nativedb/
    https://github.com/crosire/scripthookvdotnet/wiki/How-Tos

    DELETE /scripts/
    dinput8.dll
    ScriptHook.dll
    ScriptHookVDotNet.asi
*/

public class Main : Script {

    private NetworkClient NetworkClient { get; set; }
    public Chatbox ChatBox { get; set; }

    public Main() {
        NetworkClient = new NetworkClient(this);
        ChatBox = new Chatbox();

        // Register events
        this.Tick += OnTick;
        this.Tick += OnNetworkTick;

        this.KeyUp += OnKeyUp;
        this.KeyDown += OnKeyDown;  
    }

    private void OnNetworkTick(object sender, EventArgs e) {
        // SafeForNet = has been initialized (position + playerId)
        if (NetworkClient.SafeForNet) {
            // Send every tick our player AIM data
            if (Game.Player.IsAiming) {
                Vector3 camPosition = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_COORD);
                Vector3 rot = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 0);
                Vector3 dir = RotationToDirection(rot);
                Vector3 posLookAt = camPosition + dir * 10f;

                NetworkClient.SendToServerUnreliable("client_to_server::my_aim_state", NetworkClient.PlayerID + "^" + "1" + "^" + posLookAt.X + "^" + posLookAt.Y + "^" + posLookAt.Z);
            }
            else {
                NetworkClient.SendToServerUnreliable("client_to_server::my_aim_state", NetworkClient.PlayerID + "^" + "0^0^0^0");
            }

            // Send every tick our player SHOOT data
            if (Game.Player.Character.IsShooting) {
                NetworkClient.SendToServerUnreliable("client_to_server::my_shoot_state", NetworkClient.PlayerID + "^" + "1");
            }
            else if (!Game.Player.Character.IsShooting) {
                NetworkClient.SendToServerUnreliable("client_to_server::my_shoot_state", NetworkClient.PlayerID + "^" + "0");
            }

            NetworkClient.SendToServerUnreliable("client_to_server::send_my_position", NetworkClient.PlayerID + "^" + (Game.Player.Character.Position.X + 3.0f) + "^" + Game.Player.Character.Position.Y + "^" + World.GetGroundHeight(Game.Player.Character.Position));
            NetworkClient.SendToServerUnreliable("client_to_server::send_my_rotation", NetworkClient.PlayerID + "^" + Game.Player.Character.Rotation.X + "^" + Game.Player.Character.Rotation.Y + "^" + Game.Player.Character.Rotation.Z);
        }

        // We have a connection to our server!
        if (NetworkClient.NetClient.ServerConnection != null) {
            NetIncomingMessage netIncomingMessage;

            while ((netIncomingMessage = NetworkClient.NetClient.ServerConnection.Peer.ReadMessage()) != null) {
                switch (netIncomingMessage.MessageType) {
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.ErrorMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.VerboseDebugMessage:
                        string text = netIncomingMessage.ReadString();
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        NetConnectionStatus status = (NetConnectionStatus)netIncomingMessage.ReadByte();

                        if (status == NetConnectionStatus.Connected) {
                            NetworkClient.SendToServer("client_to_server::request_motd");
                            NetworkClient.SendToServer("client_to_server::request_player_id");
                        }

                        string reason = netIncomingMessage.ReadString();
                        ChatBox.Add("StatusChanged: " + status.ToString() + ": " + reason);

                        break;
                    case NetIncomingMessageType.Data:
                        string message = netIncomingMessage.ReadString();
                        NetworkClient.HandleServerMessage(message);
                        break;
                    default:
                        ChatBox.Add("Unhandled type: " + netIncomingMessage.MessageType + " " + netIncomingMessage.LengthBytes + " bytes");
                        break;
                }
                NetworkClient.NetClient.Recycle(netIncomingMessage);
            }
        }
    }

    private void OnTick(object sender, EventArgs e) {
        // DEBUG
        Game.Player.IgnoredByEveryone = true;
        Game.Player.IgnoredByPolice = true;

        

        // GUI
        ChatBox.Draw();
    }

    private void OnKeyDown(object sender, KeyEventArgs e) {
        if (e.KeyCode == Keys.F5) {
            ChatBox.Add("Hello! " + Guid.NewGuid());
        }
    }

    private void OnKeyUp(object sender, KeyEventArgs e) {
        if (e.KeyCode == Keys.NumPad0) {
            if(NetworkClient.NetClient.ServerConnection == null) {
                ChatBox.Add("Connecting to server...");
                NetworkClient.Connect();
            }
            else {
                NetworkClient.Disconnect();
                ChatBox.Add("Disconnected from server.");
            }
        }
    }

    private void ClearWorld() {
        foreach(Vehicle vehicle in World.GetNearbyVehicles(Game.Player.Character.Position, 1024f)) {
            vehicle.Delete();
        }

        foreach (Ped ped in World.GetNearbyPeds(Game.Player.Character.Position, 1024f)) {
            ped.Delete();
        }
    }

    public static Vector3 RotationToDirection(Vector3 Rotation) {
        float z = Rotation.Z;
        float num = z * 0.0174532924f;
        float x = Rotation.X;
        float num2 = x * 0.0174532924f;
        float num3 = Math.Abs((float)Math.Cos((double)num2));
        return new Vector3 {
            X = (float)((double)((float)(-(float)Math.Sin((double)num))) * (double)num3),
            Y = (float)((double)((float)Math.Cos((double)num)) * (double)num3),
            Z = (float)Math.Sin((double)num2)
        };
    }

}