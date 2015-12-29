using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Stroopwaffle {
    class Utility {
        public static Vector3 RotationToDirection(Vector3 Rotation) {
            float z = Rotation.Z;
            float num = z * 0.0174532924f;
            float x = Rotation.X;
            float num2 = x * 0.0174532924f;
            float num3 = Math.Abs((float)Math.Cos((double)num2));
            return new Vector3 {
                X = (float)((double)((float)(-(float)Math.Sin((double)num))) * (double)num3),
                Y = (float)((double)((float)Math.Cos((double)num)) * (double)num3),
                Z = (float)Math.Sin((double)num2)
            };
        }

        [DllImport("user32.dll")]
        public static extern int ToUnicode(uint virtualKeyCode, uint scanCode,
        byte[] keyboardState,
        [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)]
        StringBuilder receivingBuffer,
        int bufferSize, uint flags);

        public static string GetCharFromKey(Keys key, bool shift, bool altGr) {
            var buf = new StringBuilder(256);
            var keyboardState = new byte[256];
            if (shift)
                keyboardState[(int)Keys.ShiftKey] = 0xff;
            if (altGr) {
                keyboardState[(int)Keys.ControlKey] = 0xff;
                keyboardState[(int)Keys.Menu] = 0xff;
            }
            ToUnicode((uint)key, 0, keyboardState, buf, 256, 0);
            return buf.ToString();
        }

        public static int GetStationId() {
            if (!Game.Player.Character.IsInVehicle()) return -1;
            return Function.Call<int>(Hash.GET_PLAYER_RADIO_STATION_INDEX);
        }

        public static string GetStationName(int id) {
            return Function.Call<string>(Hash.GET_RADIO_STATION_NAME, id);
        }

        public static int GetTrackId() {
            if (!Game.Player.Character.IsInVehicle()) return -1;
            return Function.Call<int>(Hash.GET_AUDIBLE_MUSIC_TRACK_TEXT_ID);
        }

        public static bool IsVehicleEmpty(Vehicle veh) {
            if (veh == null) return true;
            if (!veh.IsSeatFree(VehicleSeat.Driver)) return false;
            for (int i = 0; i < veh.PassengerSeats; i++) {
                if (!veh.IsSeatFree((VehicleSeat)i))
                    return false;
            }
            return true;
        }

        public static Dictionary<int, int> GetVehicleMods(Vehicle veh) {
            var dict = new Dictionary<int, int>();
            for (int i = 0; i < 50; i++) {
                dict.Add(i, veh.GetMod((VehicleMod)i));
            }
            return dict;
        }

        public static Dictionary<int, int> GetPlayerProps(Ped ped) {
            var props = new Dictionary<int, int>();
            for (int i = 0; i < 15; i++) {
                var mod = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, ped.Handle, i);
                if (mod == -1) continue;
                props.Add(i, mod);
            }
            return props;
        }

        public static int GetPedSeat(Ped ped) {
            if (ped == null || !ped.IsInVehicle()) return -3;
            if (ped.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) == ped) return (int)VehicleSeat.Driver;
            for (int i = 0; i < ped.CurrentVehicle.PassengerSeats; i++) {
                if (ped.CurrentVehicle.GetPedOnSeat((VehicleSeat)i) == ped)
                    return i;
            }
            return -3;
        }

        public static int GetFreePassengerSeat(Vehicle veh) {
            if (veh == null) return -3;
            for (int i = 0; i < veh.PassengerSeats; i++) {
                if (veh.IsSeatFree((VehicleSeat)i))
                    return i;
            }
            return -3;
        }

        public static Vector3 GetLastWeaponImpact(Ped ped) {
            var coord = new OutputArgument();
            if (!Function.Call<bool>(Hash.GET_PED_LAST_WEAPON_IMPACT_COORD, ped.Handle, coord)) {
                return new Vector3();
            }
            return coord.GetResult<Vector3>();
        }
    }
}
