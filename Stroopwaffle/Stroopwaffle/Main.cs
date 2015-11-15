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
    public StatisticsUI StatisticsUI { get; set; }

    public Main() {
        NetworkClient = new NetworkClient(this);
        ChatBox = new Chatbox();
        StatisticsUI = new StatisticsUI();

        // Register events
        this.Tick += OnTick;
        this.Tick += OnNetworkTick;

        this.KeyUp += OnKeyUp;
        this.KeyDown += OnKeyDown;  
    }

    private void OnNetworkTick(object sender, EventArgs e) {
        // We have a connection to our server!
        if (NetworkClient.NetClient.ServerConnection != null) {
            NetworkClient.ReadPackets();

            if (NetworkClient.SafeForNet) {
                NetworkClient.WritePackets();
            }
        }   
    }

    private void OnTick(object sender, EventArgs e) {
        // DEBUG
        Game.Player.IgnoredByEveryone = true;
        Game.Player.IgnoredByPolice = true;      

        // GUI
        ChatBox.Draw();
        StatisticsUI.Draw();
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