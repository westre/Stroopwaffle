﻿using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.Math;
using Stroopwaffle;
using Lidgren.Network;
using Stroopwaffle_Shared;
using System.Collections.Generic;

/*
    http://www.dev-c.com/nativedb/
    https://github.com/crosire/scripthookvdotnet/wiki/How-Tos

    DELETE /scripts/
    dinput8.dll
    ScriptHook.dll
    ScriptHookVDotNet.asi

    Some interesting articles
    http://etodd.io/2011/12/10/c-for-scripting-runtime-compilation/
    https://github.com/NLua/NLua
    http://stackoverflow.com/questions/24927776/nlua-luainterface-calling-a-function
    http://stackoverflow.com/questions/137933/what-is-the-best-scripting-language-to-embed-in-a-c-sharp-desktop-application
    http://stackoverflow.com/questions/1650648/catching-ctrl-c-in-a-textbox
*/

public class Main : Script {

    public NetworkClient NetworkClient { get; set; }
    public Chatbox ChatBox { get; set; }
    public StatisticsUI StatisticsUI { get; set; }

    public static bool CloneSync { get; set; } = true; 

    public Main() {
        NetworkClient = new NetworkClient(this);
        ChatBox = new Chatbox(this);
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

        List<NetworkVehicle> safeVehicleList = new List<NetworkVehicle>(NetworkClient.Vehicles);
        foreach(NetworkVehicle networkVehicle in safeVehicleList) {
            Point labelPosition = UI.WorldToScreen(networkVehicle.PhysicalVehicle.Position + new Vector3(0, 0, 0.5f));

            UIElement text = new UIText("ID " + networkVehicle.ID + "\nPID: " + networkVehicle.PlayerID, labelPosition, 0.3f, Color.BlueViolet);
            text.Draw();
        }

        List<NetworkPlayer> safePlayerList = new List<NetworkPlayer>(NetworkClient.ServerPlayers);
        foreach (NetworkPlayer networkPlayer in safePlayerList) {
            Point labelPosition = UI.WorldToScreen(networkPlayer.Position + new Vector3(0, 0, 2.5f));

            UIElement text = new UIText("ID " + networkPlayer.PlayerID + "\n" + networkPlayer.Health + "/" + networkPlayer.MaxHealth + " (" + networkPlayer.Armor + ")", labelPosition, 0.3f, Color.Green);
            text.Draw();
        }

        // GUI
        ChatBox.Draw();
        StatisticsUI.Draw();

        ChatBox.ScaleForm.Render2D();
        ChatBox.Tick();

        Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
        Function.Call(Hash.SET_RANDOM_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
        Function.Call(Hash.SET_PARKED_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
        Function.Call(Hash.SET_PED_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
        Function.Call(Hash.SET_SCENARIO_PED_DENSITY_MULTIPLIER_THIS_FRAME, 0f, 0f);
        Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 19, 1); // Disable character wheel
        Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 44, 1); // Disable cover
        Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 171, 1); // Disable INPUT_SPECIAL_ABILITY_PC
        Function.Call(Hash.SET_MAX_WANTED_LEVEL, 0);
        Function.Call(Hash.SET_GARBAGE_TRUCKS, 0);
        Function.Call(Hash.SET_RANDOM_BOATS, 0);
        Function.Call(Hash.SET_RANDOM_TRAINS, 0);
    }

    private void OnKeyDown(object sender, KeyEventArgs e) {
        ChatBox.KeyDown(sender, e);
    }

    private void OnKeyUp(object sender, KeyEventArgs e) {
        if (e.KeyCode == Keys.NumPad0) {
            if(NetworkClient.NetClient.ServerConnection == null) {
                ChatBox.Add("Connecting to server...");

                NetConnection connection = NetworkClient.Connect("127.0.0.1");
                if (connection != null) {
                    ChatBox.Add("Connected");
                    InitializeGame();
                }
            }
            else {
                NetworkClient.Disconnect();
                ChatBox.Add("Disconnected from server.");
            }
        }
        if (e.KeyCode == Keys.NumPad1) {
            if (NetworkClient.NetClient.ServerConnection == null) {
                ChatBox.Add("Connecting to external server...");

                NetConnection connection = NetworkClient.Connect("192.168.1.133");
                if (connection != null) {
                    ChatBox.Add("Connected");
                    InitializeGame();
                }
            }
            else {
                NetworkClient.Disconnect();
                ChatBox.Add("Disconnected from server.");
            }
        }
    }

    private void InitializeGame() {
        foreach(Vehicle vehicle in World.GetAllVehicles()) {
            vehicle.Delete();
        }

        foreach (Ped ped in World.GetAllPeds()) {
            ped.Delete();
        }

        foreach(Blip blip in World.GetActiveBlips()) {
            blip.Remove();
        }
    }
}