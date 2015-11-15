using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Stroopwaffle_Server {
    public partial class ServerForm : Form {
        delegate void SetTextCallback(string text);

        public ServerForm() {
            InitializeComponent();
        }

        public void Output(string message) {
            if (this.messageLog.InvokeRequired) {
                SetTextCallback d = new SetTextCallback(Output);
                this.Invoke(d, new object[] { message });
            }
            else {
                messageLog.Items.Add(message);
            }
        }
    }
}
