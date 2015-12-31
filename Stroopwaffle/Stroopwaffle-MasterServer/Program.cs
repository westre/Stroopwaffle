using Lidgren.Network;
using Stroopwaffle_Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Stroopwaffle_MasterServer {
    class Program {
        static void Main(string[] args) {
            Dictionary<long, ServerData> registeredHosts = new Dictionary<long, ServerData>();

            NetPeerConfiguration config = new NetPeerConfiguration("masterserver");
            config.SetMessageTypeEnabled(NetIncomingMessageType.UnconnectedData, true);
            config.Port = SharedConfiguration.MasterServerEndPoint.Port;

            NetPeer peer = new NetPeer(config);
            peer.Start();

            // keep going until ESCAPE is pressed
            Console.WriteLine("Press ESC to quit");
            while (!Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape) {
                NetIncomingMessage msg;
                while ((msg = peer.ReadMessage()) != null) {
                    switch (msg.MessageType) {
                        case NetIncomingMessageType.UnconnectedData:
                            //
                            // We've received a message from a client or a host
                            //

                            // by design, the first byte always indicates action
                            switch ((MasterServerMessageType)msg.ReadByte()) {
                                case MasterServerMessageType.RegisterHost:

                                    // It's a host wanting to register its presence
                                    var id = msg.ReadInt64(); // server unique identifier
                                    Console.WriteLine("Got registration for host " + id);

                                    ServerData serverData = new ServerData();
                                    serverData.IPEndPoint[0] = msg.ReadIPEndPoint();
                                    serverData.IPEndPoint[1] = msg.SenderEndPoint;

                                    serverData.ServerName = msg.ReadString();
                                    serverData.ScriptName = msg.ReadString();
                                    serverData.Players = msg.ReadInt32();
                                    serverData.MaxPlayers = msg.ReadInt32();

                                    registeredHosts[id] = serverData;
                                    break;

                                case MasterServerMessageType.RequestHostList:
                                    // It's a client wanting a list of registered hosts
                                    Console.WriteLine("Sending list of " + registeredHosts.Count + " hosts to client " + msg.SenderEndPoint);
                                    foreach (var kvp in registeredHosts) {
                                        // send registered host to client
                                        NetOutgoingMessage om = peer.CreateMessage();
                                        om.Write(kvp.Key);
                                        om.Write(kvp.Value.IPEndPoint[0]);
                                        om.Write(kvp.Value.IPEndPoint[1]);
                                        om.Write(kvp.Value.ServerName);
                                        om.Write(kvp.Value.ScriptName);
                                        om.Write(kvp.Value.Players);
                                        om.Write(kvp.Value.MaxPlayers);
                                        peer.SendUnconnectedMessage(om, msg.SenderEndPoint);
                                    }

                                    break;
                            }
                            break;

                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.ErrorMessage:
                            // print diagnostics message
                            Console.WriteLine(msg.ReadString());
                            break;
                    }
                }
            }

            peer.Shutdown("shutting down");
        }
    }
}
