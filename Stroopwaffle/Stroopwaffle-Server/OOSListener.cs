using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stroopwaffle_Shared;

namespace Stroopwaffle_Server {
    class OOSListener {
        private API API { get; set; }

        public OOSListener(API API) {
            this.API = API;
        }

        public void Process(NetworkPlayer networkPlayer, OOSPacket oosPacket) {
            switch(oosPacket) {
                case OOSPacket.InvalidVehicle:
                    API.Fire(API.Callback.OnOOS, "INVALID_VEHICLE", networkPlayer.PlayerID);
                    break;
                case OOSPacket.InvalidWeapon:
                    API.Fire(API.Callback.OnOOS, "INVALID_WEAPON", networkPlayer.PlayerID);
                    break;
                case OOSPacket.InvalidModel:
                    API.Fire(API.Callback.OnOOS, "INVALID_MODEL", networkPlayer.PlayerID);
                    break;
            }
        }
    }
}
