using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using GTA;
using GTA.Math;

namespace Stroopwaffle {
    class NetworkPlayer {

        public string Name { get; set; }
        public int PlayerID { get; set; }
        public Ped Ped { get; set; }
        public Vector3 Position { get; set; }
        public bool LocalPlayer { get; set; }
        public Vector3 Rotation { get; set; }
        public int Aiming { get; set; }
        public Vector3 AimPosition { get; set; }
        public int Shooting { get; set; }

        public NetworkPlayer() {
            
        }
    }
}
