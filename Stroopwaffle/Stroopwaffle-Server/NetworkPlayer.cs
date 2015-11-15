﻿using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stroopwaffle_Server {
    public class NetworkPlayer {
        public NetConnection NetConnection { get; set; }
        public int PlayerID { get; set; }
        public Vector3 Position { get; set; }
        public bool SafeForNet { get; set; }
        public Vector3 Rotation { get; set; }
        public int Aiming { get; set; }
        public Vector3 AimLocation { get; set; }
        public int Shooting { get; set; }

        public NetworkPlayer(NetConnection netConnection) {
            NetConnection = netConnection;
        }
    }
}
