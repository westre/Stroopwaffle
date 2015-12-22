using GTA;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Stroopwaffle {
    public class StatisticsUI : UIContainer {
        public StatisticsUI() : base(new Point(UI.WIDTH - 90, UI.HEIGHT - 30), new Size(90, 30), Color.FromArgb(100, 0, 0, 0)) {
            Items.Add(new UIText("AMP alpha 1", new Point(5, 5), 0.3f));
        }

        public void SetSentBytes(long bytes) {
            ((UIText)Items[0]).Caption = bytes + "B/s";
        }
    }
}
