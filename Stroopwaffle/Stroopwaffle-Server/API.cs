﻿using Lidgren.Network;
using NLua;
using Stroopwaffle_Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//http://stackoverflow.com/questions/29873182/instance-of-c-sharp-class-with-nlua/29900580#29900580
//http://stackoverflow.com/questions/22666142/how-to-use-c-sharp-static-string-with-nlua/22672113#22672113

namespace Stroopwaffle_Server {
    class API {
        private Server Server { get; set; }
        public Lua Lua { get; set; }

        public enum Callback {
            OnScriptInitialize,
            OnScriptExit,
            OnPlayerConnect,
            OnPlayerDisconnect,
            OnPlayerChat
        }

        public API(Server server) {
            Server = server;
            Lua = new Lua();

            RegisterAllFunctions();
        }

        private void RegisterAllFunctions() {
            Lua.RegisterFunction("broadcastMessage", this, typeof(API).GetMethod("API_broadcastMessage"));
            Lua.RegisterFunction("broadcastPlayerMessage", this, typeof(API).GetMethod("API_broadcastPlayerMessage"));
            Lua.RegisterFunction("createVehicle", this, typeof(API).GetMethod("API_createVehicle"));
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
        public void API_broadcastMessage(string message) {
            Server.Form.Output("API_DEBUG: " + message);
            Server.SendBroadcastMessagePacket(message);
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

        public void API_createVehicle(int vehicleHash, int posX, int posY, int posZ, int rotX, int rotY, int rotZ, int rotW) {
            NetworkVehicle networkVehicle = new NetworkVehicle();
            networkVehicle.Hash = vehicleHash;
            networkVehicle.PosX = posX;
            networkVehicle.PosY = posY;
            networkVehicle.PosZ = posZ;
            networkVehicle.RotX = rotX;
            networkVehicle.RotY = rotY;
            networkVehicle.RotZ = rotZ;
            networkVehicle.RotW = rotW;

            Server.RegisterVehicle(networkVehicle);
        }
    }
}
