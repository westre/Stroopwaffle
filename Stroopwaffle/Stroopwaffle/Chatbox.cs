using GTA;
using System.Collections.Generic;
using System.Drawing;

namespace Stroopwaffle {
    public class Chatbox : UIContainer {
        public Chatbox() : base(new Point(10, 10), new Size(400, 160), Color.FromArgb(100, 0, 0, 0)) {
            int x = 5;
            int y = 5;
            for(int index = 0; index < 10; index++) {
                Items.Add(new UIText("", new Point(x, y), 0.3f));
                y += 15;
            }
        }

        public void Add(string message) {
            for(int line = 0; line < 9; line++) {
                ((UIText)Items[line]).Caption = ((UIText)Items[line + 1]).Caption;
            }
            ((UIText)Items[9]).Caption = message;
        }
    }
}
