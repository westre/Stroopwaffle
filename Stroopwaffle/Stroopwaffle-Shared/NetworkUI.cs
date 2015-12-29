using GTA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stroopwaffle_Shared {
    public class NetworkUI {
        public byte A { get; set; }
        public byte B { get; set; }
        public byte G { get; set; }
        public int ID { get; set; }
        public int PlayerId { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public byte R { get; set; }
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public byte Type { get; set; }

        // Clientside only
        public UIElement UIElement { get; set; }
    }
}
