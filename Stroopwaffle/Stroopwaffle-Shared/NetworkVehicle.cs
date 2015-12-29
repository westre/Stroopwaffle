using GTA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stroopwaffle_Shared {
    public class NetworkVehicle {
        public int ID { get; set; }
        public int PlayerID { get; set; }
        public int Hash { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float RotW { get; set; }
        public float RotX { get; set; }
        public float RotY { get; set; }
        public float RotZ { get; set; }
        public int PrimaryColor { get; set; }
        public int SecondaryColor { get; set; }
        public float Speed { get; set; }

        // Clientside only
        public Vehicle PhysicalVehicle { get; set; }
        public int Seat { get; set; }

        public static NetworkVehicle Exists(List<NetworkVehicle> vehicles, int id) {
            foreach(NetworkVehicle vehicle in vehicles) {
                if (vehicle.ID == id)
                    return vehicle;
            }
            return null;
        }
    }
}
