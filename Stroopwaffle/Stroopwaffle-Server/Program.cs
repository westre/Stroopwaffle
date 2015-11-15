using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Stroopwaffle_Server {
    static class Program {

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ServerForm serverForm = new ServerForm();

            NetPeerConfiguration config = new NetPeerConfiguration("wh");
            config.MaximumConnections = 100;
            config.Port = 80;
            Server server = new Server(serverForm, config);

            Application.Idle += server.Application_Idle;
            Application.Run(serverForm);
        }
    }
}
