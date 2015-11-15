using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Stroopwaffle_DummyClient {
    public partial class Form1 : Form {
        private NetworkPlayer netPlayer;

        delegate void SetTextCallback(string text);

        public Form1() {
            InitializeComponent();

            netPlayer = new NetworkPlayer(this);
            netPlayer.Connect();
        }

        public void Output(string message) {
            if (this.listBox1.InvokeRequired) {
                SetTextCallback d = new SetTextCallback(Output);
                this.Invoke(d, new object[] { message });
            }
            else {
                listBox1.Items.Add(message);
            }
        }
    }
}
