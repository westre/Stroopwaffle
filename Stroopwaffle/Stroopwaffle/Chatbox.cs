using GTA;
using GTA.Native;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Stroopwaffle {
    public class Chatbox : UIContainer {
        private Main Main { get; set; }
        public Scaleform ScaleForm { get; set; }
        private bool focused;       
        public string InputString { get; set; }

        public Chatbox(Main Main) : base(new Point(10, 10), new Size(400, 160), Color.FromArgb(0, 0, 0, 0)) {
            this.Main = Main;

            int x = 5;
            int y = 5;
            for(int index = 0; index < 10; index++) {
                Items.Add(new UIText("", new Point(x, y), 0.3f));
                y += 15;
            }

            ScaleForm = new Scaleform(0);
            ScaleForm.Load("multiplayer_chat");

            ScaleForm.CallFunction("SET_FOCUS", 2, 2, "ALL");
            ScaleForm.CallFunction("SET_FOCUS", 1, 2, "ALL");
        }

        public void Add(string message) {
            for(int line = 0; line < 9; line++) {
                ((UIText)Items[line]).Caption = ((UIText)Items[line + 1]).Caption;
            }
            ((UIText)Items[9]).Caption = message;
        }

        public void Tick() {
            if(Focused) {
                Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 1);
            }
        }

        public bool Focused {
            get { return focused; }
            set {
                if (value && !focused) {
                    ScaleForm.CallFunction("SET_FOCUS", 2, 2, "ALL");
                }
                else if (!value && focused) {
                    ScaleForm.CallFunction("SET_FOCUS", 1, 2, "ALL");
                }
                focused = value;
            }
        }

        public void KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Back) {
                ScaleForm.CallFunction("SET_FOCUS", 1, 2, "ALL");
                ScaleForm.CallFunction("SET_FOCUS", 2, 2, "ALL");

                if (InputString.Length > 0) {
                    InputString = InputString.Substring(0, InputString.Length - 1);
                    ScaleForm.CallFunction("ADD_TEXT", InputString);
                }
            }
            else if (e.KeyCode == Keys.Enter) {
                Main.NetworkClient.SendStringMessagePacket(InputString);
                InputString = "";
                Focused = false;
            }
            else if (e.KeyCode == Keys.T && !Focused && Main.NetworkClient.NetClient.ServerConnection != null) {
                ScaleForm.CallFunction("SET_FOCUS", 1, 2, "ALL");
                ScaleForm.CallFunction("SET_FOCUS", 2, 2, "ALL");
                ScaleForm.CallFunction("ADD_TEXT", "");
                Focused = true;
            }
            else if (e.KeyCode == Keys.Escape) {
                InputString = "";
                Focused = false;
            }      
            else if (Focused) {
                string keyChar = Utility.GetCharFromKey(e.KeyCode, Game.IsKeyPressed(Keys.ShiftKey), false);

                InputString += keyChar;
                ScaleForm.CallFunction("ADD_TEXT", keyChar);
            }
        }
    }
}
