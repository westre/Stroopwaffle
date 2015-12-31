using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Stroopwaffle_MasterServer {
    class ServerData {
        public IPEndPoint[] IPEndPoint = new IPEndPoint[2];
        public string ServerName;
        public string ScriptName;
        public int Players;
        public int MaxPlayers;
    }
}
