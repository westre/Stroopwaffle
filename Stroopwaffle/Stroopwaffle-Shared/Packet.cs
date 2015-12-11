using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stroopwaffle_Shared {
    public enum PacketType {
        Initialization,
        Position,
        Rotation,
        Shooting,
        Aiming,
        TotalPlayerData,
        Deinitialization,
        ChatMessage,
        Vehicle,
        NoVehicle
    }
}
