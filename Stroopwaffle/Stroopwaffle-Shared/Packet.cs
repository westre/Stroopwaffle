using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stroopwaffle_Shared {

    public enum Rate {
        ServerTick = 10, // 1/30 = 33Hz
        PedInterpolation = 7, // ms in future
        VehicleInterpolation = 2000
    }

    public enum PacketType {
        Initialization,
        Position,
        Rotation,
        Shooting,
        Aiming,
        TotalPlayerData,
        Deinitialization,
        ChatMessage,
        NoVehicle,
        TotalVehicleData,
        CurrentWeapon,
        SetPedPosition,
        SetPlayerArmor,
        SetPlayerHealth,
        NewPed,
        GivePlayerWeapon,
        OOSPacket,
        SetPlayerModel,
        GUIPacket
    }

    public enum OOSPacket {
        InvalidVehicle,
        InvalidWeapon,
        InvalidModel
    }

    public enum GUIPacket {
        Rectangle,
        RemoveGUIElement
    }

    public enum MasterServerMessageType {
        RegisterHost,
        RequestHostList,
        RequestIntroduction,
    }
}
