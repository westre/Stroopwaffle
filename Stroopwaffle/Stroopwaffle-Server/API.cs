using NLua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Lua.RegisterFunction("broadcastMessage", Server, typeof(Server).GetMethod("SendBroadcastMessagePacket"));
        }

        public void OnScriptInitialize() {
            LuaFunction function = (LuaFunction)Lua["OnScriptInitialize"];
            if (function != null) {
                function.Call();
            }
        }

        public void Fire(Callback callback, params object[] values) {
            LuaFunction function = (LuaFunction)Lua[callback.ToString()];
            if(function != null) {
                function.Call(values);
            }
        }
    }
}
