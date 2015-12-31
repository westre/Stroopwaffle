using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Stroopwaffle_Launcher {
    /// <summary>
    /// Interaction logic for ServerEntry.xaml
    /// </summary>
    public partial class ServerEntry : UserControl {
        public string Version { get; set; }
        public string ServerName { get; set; }
        public int Players { get; set; }
        public int MaxPlayers { get; set; }
        public int Ping { get; set; }
        public string Script { get; set; }

        public ServerEntry(string version, string name, int players, int maxPlayers, int ping, string script) {
            Version = version;
            ServerName = name;
            Players = players;
            MaxPlayers = maxPlayers;
            Ping = ping;
            Script = script;

            InitializeComponent();
        }

        public void UpdateView() {
            lblName.Content = ServerName;
            lblPlayers.Content = Players + "/" + MaxPlayers;
            lblVersion.Content = Version;
            lblPing.Content = Ping + "ms";
            lblScript.Content = Script;
        }
    }
}
