using Lidgren.Network;
using NLua;
using Stroopwaffle_Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//http://stackoverflow.com/questions/29873182/instance-of-c-sharp-class-with-nlua/29900580#29900580
//http://stackoverflow.com/questions/22666142/how-to-use-c-sharp-static-string-with-nlua/22672113#22672113

namespace Stroopwaffle_Server {
    class ScriptTimer {
        public string Function { get; set; }
        public DateTime ExecuteOn { get; set; }
        public LuaTable Arguments { get; set; }
    }

    class API {
        private Server Server { get; set; }
        public Lua Lua { get; set; }
        private List<ScriptTimer> ScriptTimers { get; set; }

        public enum Callback {
            OnScriptInitialize,
            OnScriptExit,
            OnPlayerConnect,
            OnPlayerDisconnect,
            OnPlayerChat,
            OnPlayerInitialize,
            OnPlayerDied,
            OnPlayerRespawn,
            OnOOS,
            OnUpdate,
            OnPlayerEnterVehicle,
            OnPlayerExitVehicle
        }

        public API(Server server) {
            Server = server;
            Lua = new Lua();
            ScriptTimers = new List<ScriptTimer>();

            RegisterAllFunctions();
        }

        public void Tick() {
            List<ScriptTimer> safeScriptTimers = new List<ScriptTimer>(ScriptTimers);
            DateTime now = DateTime.Now;

            foreach(ScriptTimer scriptTimer in safeScriptTimers) {
                if(now >= scriptTimer.ExecuteOn) {
                    LuaFunction function = (LuaFunction)Lua[scriptTimer.Function];

                    if (function != null) {
                        function.Call(scriptTimer.Arguments);

                        ScriptTimers.Remove(scriptTimer);
                    }
                }
            }

            Fire(Callback.OnUpdate);
        }

        private void RegisterAllFunctions() {
            Lua.RegisterFunction("broadcastMessage", this, typeof(API).GetMethod("API_broadcastMessage")); 
            Lua.RegisterFunction("broadcastPlayerMessage", this, typeof(API).GetMethod("API_broadcastPlayerMessage"));

            Lua.RegisterFunction("createVehicle", this, typeof(API).GetMethod("API_createVehicle"));
            Lua.RegisterFunction("destroyVehicle", this, typeof(API).GetMethod("API_destroyVehicle"));

            Lua.RegisterFunction("setPlayerPosition", this, typeof(API).GetMethod("API_setPlayerPosition"));
            Lua.RegisterFunction("getPosition", this, typeof(API).GetMethod("API_getPosition"));

            Lua.RegisterFunction("getRotation", this, typeof(API).GetMethod("API_getRotation"));
         
            Lua.RegisterFunction("setPlayerVisible", this, typeof(API).GetMethod("API_setPlayerVisible"));
            Lua.RegisterFunction("freezePlayer", this, typeof(API).GetMethod("API_freezePlayer"));
            Lua.RegisterFunction("setPlayerHealth", this, typeof(API).GetMethod("API_setPlayerHealth"));
            Lua.RegisterFunction("setPlayerArmor", this, typeof(API).GetMethod("API_setPlayerArmor"));
            Lua.RegisterFunction("givePlayerWeapon", this, typeof(API).GetMethod("API_givePlayerWeapon"));
            Lua.RegisterFunction("setPlayerModel", this, typeof(API).GetMethod("API_setPlayerModel"));

            Lua.RegisterFunction("setTimer", this, typeof(API).GetMethod("API_setTimer"));
            Lua.RegisterFunction("consolePrint", this, typeof(API).GetMethod("API_consolePrint"));

            Lua.RegisterFunction("guiCreateRectangle", this, typeof(API).GetMethod("API_guiCreateRectangle"));
            Lua.RegisterFunction("removeGui", this, typeof(API).GetMethod("API_removeGui"));
        }

        public void Fire(Callback callback, params object[] values) {
            try {
                LuaFunction function = (LuaFunction)Lua[callback.ToString()];
                if (function != null) {
                    function.Call(values);
                }
            }
            catch(Exception ex) {
                Server.Form.Output(ex.ToString());
            }
        }

        public void Load(string fileName) {
            Lua.DoFile(fileName);
        }

        // All functions
        public int API_guiCreateRectangle(int playerId, int posX, int posY, int sizeX, int sizeY, byte r, byte g, byte b, byte a) {
            NetConnection netConnection = Server.Find(playerId);
            //Server.SendGUICreateRectanglePacket(netConnection, playerId, new Point(posX, posY), new Size(sizeX, sizeY), Color.FromArgb(a, r, g, b));

            NetworkUI networkUI = new NetworkUI();
            networkUI.Type = (byte)GUIPacket.Rectangle;
            networkUI.PlayerId = playerId;
            networkUI.PosX = posX;
            networkUI.PosY = posY;
            networkUI.SizeX = sizeX;
            networkUI.SizeY = sizeY;
            networkUI.R = r;
            networkUI.G = g;
            networkUI.B = b;
            networkUI.A = a;

            int id = Server.RegisterNetworkUI(networkUI);

            return id;
        }

        public void API_removeGui(int networkUIId) {
            NetworkUI networkUi = NetworkUI.Get(Server.UIs, networkUIId);
            NetConnection netConnection = Server.Find(networkUi.PlayerId);

            if (networkUi != null) {
                Server.UIs.Remove(networkUi);
                Server.SendGUIRemovePacket(netConnection, networkUi.PlayerId, networkUi.ID);
                Server.Form.Output("NetworkUI " + networkUi + " removed from playerId: " + networkUi.PlayerId);
            }
            else {
                Server.Form.Output("uiID " + networkUIId + " is null");
            }
        }

        public void API_createVehicle(int vehicleHash, int posX, int posY, int posZ, int rotX, int rotY, int rotZ) {
            NetworkVehicle networkVehicle = new NetworkVehicle();
            networkVehicle.Hash = vehicleHash;
            networkVehicle.PosX = posX;
            networkVehicle.PosY = posY;
            networkVehicle.PosZ = posZ;
            networkVehicle.RotX = rotX;
            networkVehicle.RotY = rotY;
            networkVehicle.RotZ = rotZ;
            networkVehicle.RotW = 0;
            networkVehicle.PrimaryColor = 0;
            networkVehicle.SecondaryColor = 0;

            Server.RegisterVehicle(networkVehicle);
        }

        public void API_broadcastMessage(string message) {
            Server.Form.Output("API_DEBUG: " + message);
            Server.SendBroadcastMessagePacket(message);
        }

        public void API_setTimer(int milliseconds, string callback, LuaTable luaTable) {
            DateTime execute = DateTime.Now;
            execute = execute.AddMilliseconds(milliseconds);

            ScriptTimers.Add(new ScriptTimer() {
                ExecuteOn = execute,
                Function = callback,
                Arguments = luaTable
            });
        }

        public void API_consolePrint(string message) {
            Server.Form.Output(message);
        }

        public void API_broadcastPlayerMessage(int playerId, string message) {
            NetConnection netConnection = Server.Find(playerId);

            if(netConnection != null) {
                Server.SendBroadcastMessagePacket(netConnection, message);
            }
            else {
                Server.Form.Output("Error, API_broadcastPlayerMessage -> netConnection is null");
            }          
        }

        public void API_destroyVehicle(int vehicleId) {
            NetworkVehicle networkVehicle = NetworkVehicle.Exists(Server.Vehicles, vehicleId);

            if(networkVehicle != null) {
                Server.Vehicles.Remove(networkVehicle);
                Server.Form.Output("NetworkVehicle " + vehicleId + " removed");
            }
            else {
                Server.Form.Output("VehicleId " + vehicleId + " is null");
            }
        }

        public LuaTable API_getPosition(int playerId) {
            NetworkPlayer networkPlayer = NetworkPlayer.Get(Server.Players, playerId);
            if(networkPlayer != null) {
                Server.Form.Output("X: " + networkPlayer.Position.X + ", Y: " + networkPlayer.Position.Y + ", Z: " + networkPlayer.Position.Z);
            }

            Lua.DoString("tempTable = {}");
            Lua.DoString("table.insert(tempTable, \"" + networkPlayer.Position.X + "\")");
            Lua.DoString("table.insert(tempTable, \"" + networkPlayer.Position.Y + "\")");
            Lua.DoString("table.insert(tempTable, \"" + networkPlayer.Position.Z + "\")");
            LuaTable tab = Lua.GetTable("tempTable");

            return tab;
        }

        public void API_getRotation(int playerId) {

        }

        public void API_setPlayerPosition(int playerId, float posX, float posY, float posZ) {
            NetworkPlayer networkPlayer = NetworkPlayer.Get(Server.Players, playerId);
            NetConnection netConnection = Server.Find(playerId);

            if (networkPlayer != null) {
                networkPlayer.Position = new Vector3(posX, posY, posZ);
                Server.SendSetPlayerPositionPacket(netConnection, networkPlayer.PlayerID, networkPlayer.Position);
            }
        }

        public void API_setPlayerModel(int playerId, uint model) {
            NetworkPlayer networkPlayer = NetworkPlayer.Get(Server.Players, playerId);

            if (networkPlayer != null) {
                networkPlayer.Model = model;
                Server.SendSetPlayerModelPacket(networkPlayer.PlayerID, networkPlayer.Model);
            }
        }

        // Change this to RPC
        public void API_setPlayerVisible(int playerId, bool visibility) {
            NetworkPlayer networkPlayer = NetworkPlayer.Get(Server.Players, playerId);
            if (networkPlayer != null) {
                networkPlayer.Visible = visibility;
            }
        }

        // Change this to RPC
        public void API_freezePlayer(int playerId, bool freeze) {
            NetworkPlayer networkPlayer = NetworkPlayer.Get(Server.Players, playerId);
            if (networkPlayer != null) {
                networkPlayer.Frozen = freeze;
            }
        }

        public void API_setPlayerHealth(int playerId, int health) {
            NetworkPlayer networkPlayer = NetworkPlayer.Get(Server.Players, playerId);

            if (networkPlayer != null) {
                networkPlayer.Health = health;
                Server.SendSetPlayerHealthPacket(networkPlayer.PlayerID, networkPlayer.Health);
            }
        }

        public void API_setPlayerArmor(int playerId, int armor) {
            NetworkPlayer networkPlayer = NetworkPlayer.Get(Server.Players, playerId);

            if (networkPlayer != null) {
                networkPlayer.Armor = armor;
                Server.SendSetPlayerArmorPacket(networkPlayer.PlayerID, networkPlayer.Armor);
            }
        }

        public void API_givePlayerWeapon(int playerId, int weaponId) {
            NetworkPlayer networkPlayer = NetworkPlayer.Get(Server.Players, playerId);

            if (networkPlayer != null) {
                networkPlayer.Weapons.Add(weaponId);
                Server.SendGivePlayerWeaponPacket(networkPlayer.PlayerID, weaponId);
            }
        }
    }
}
