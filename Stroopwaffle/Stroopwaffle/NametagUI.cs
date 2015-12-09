using GTA;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stroopwaffle {

    public struct Nametag {
        public NetworkPlayer NetworkPlayer { get; set; }
        public Point Point { get; set; }
    }

    public class NametagUI : UIContainer {
        public Main Main;
        public List<Nametag> Nametags { get; set; }

        public NametagUI(Main Main) : base(new Point(0, 0), new Size(UI.WIDTH, UI.HEIGHT), Color.FromArgb(0, 0, 0, 0)) {
            this.Main = Main;
            Nametags = new List<Nametag>();
        }


        public void SetNametagForPlayer(Nametag playerNametag) {
            if(Items.Count > 0) {
                foreach (UIElement uiElement in Items) {
                    if (uiElement is UIText) {
                        UIText nameTag = (UIText)uiElement;

                        if (nameTag.Caption == "ID: " + playerNametag.NetworkPlayer.PlayerID.ToString()) {
                            nameTag.Position = playerNametag.Point;
                        }
                        else {
                            Items.Add(new UIText("ID: " + playerNametag.NetworkPlayer.PlayerID.ToString(), playerNametag.Point, 0.3f));
                        }
                    }
                }
            }
            else {
                Items.Add(new UIText("ID: " + playerNametag.NetworkPlayer.PlayerID.ToString(), playerNametag.Point, 0.3f));
            }
        }
    }
}
