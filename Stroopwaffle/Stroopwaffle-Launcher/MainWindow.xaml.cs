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
using System.Configuration;
using Lidgren.Network;
using System.Net;
using System.Threading;
using Stroopwaffle_Shared;

namespace Stroopwaffle_Launcher {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private static NetClient m_client;
        private static IPEndPoint m_masterServer;
        private static Dictionary<long, IPEndPoint[]> m_hostList;

        public MainWindow() {
            InitializeComponent();

            m_hostList = new Dictionary<long, IPEndPoint[]>();

            NetPeerConfiguration config = new NetPeerConfiguration("game");
            config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
            m_client = new NetClient(config);
            m_client.Start();

            Thread thread = new Thread(delegate() {
                while(true) {
                    NetIncomingMessage inc;
                    while ((inc = m_client.ReadMessage()) != null) {
                        switch (inc.MessageType) {
                            case NetIncomingMessageType.VerboseDebugMessage:
                            case NetIncomingMessageType.DebugMessage:
                            case NetIncomingMessageType.WarningMessage:
                            case NetIncomingMessageType.ErrorMessage:
                                break;
                            case NetIncomingMessageType.UnconnectedData:
                                if (inc.SenderEndPoint.Equals(m_masterServer)) {
                                    // it's from the master server - must be a host
                                    var id = inc.ReadInt64();
                                    var hostInternal = inc.ReadIPEndPoint();
                                    var hostExternal = inc.ReadIPEndPoint();

                                    string serverName = inc.ReadString();
                                    string scriptName = inc.ReadString();
                                    int players = inc.ReadInt32();
                                    int maxPlayers = inc.ReadInt32();

                                    m_hostList[id] = new IPEndPoint[] { hostInternal, hostExternal };

                                    Application.Current.Dispatcher.Invoke(delegate {
                                        ServerEntry serverEntry = null;
                                        foreach(ServerEntry entry in spServerList.Children) {
                                            if(Convert.ToInt64(entry.Tag) == id) {
                                                serverEntry = entry;
                                            }                                           
                                        }

                                        if(serverEntry == null) {
                                            serverEntry = new ServerEntry("v0.1", serverName, players, maxPlayers, -1, scriptName);
                                            serverEntry.Tag = id;
                                            serverEntry.UpdateView();
                                            spServerList.Children.Add(serverEntry);
                                        }
                                        else {
                                            serverEntry.ServerName = serverName;
                                            serverEntry.Players = players;
                                            serverEntry.Script = scriptName;
                                            serverEntry.MaxPlayers = maxPlayers;
                                            serverEntry.UpdateView();
                                        }
                                    });                                 
                                }
                                break;
                        }
                    }
                }      
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void BtnMultiplayer_Click(object sender, RoutedEventArgs e) {
            throw new NotImplementedException();
        }

        private void TestButtonClick(object sender, RoutedEventArgs e) {
            ServerEntry serverEntry = new ServerEntry("v0.1", "dev server", 0, 32, -1, "Test script");
            serverEntry.UpdateView();
            spServerList.Children.Add(serverEntry);
        }

        private void RefreshServerList(object sender, RoutedEventArgs e) {
            //
            // Send request for server list to master server
            //
            m_masterServer = new IPEndPoint(NetUtility.Resolve(SharedConfiguration.MasterServerEndPoint.Address.ToString()), SharedConfiguration.MasterServerEndPoint.Port);

            NetOutgoingMessage listRequest = m_client.CreateMessage();
            listRequest.Write((byte)MasterServerMessageType.RequestHostList);
            m_client.SendUnconnectedMessage(listRequest, m_masterServer);
        }
    }
}
