using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stroopwaffle_DummyClient {
    class NetworkPlayer {
        public NetClient client;
        public bool host;
        public int playerId;

        private Form1 form;

        public NetworkPlayer(Form1 form) {
            this.form = form;
        }

        public void Connect() {
            NetPeerConfiguration config = new NetPeerConfiguration("wh");
            config.AutoFlushSendQueue = false;

            client = new NetClient(config);
            client.RegisterReceivedCallback(new SendOrPostCallback(ReceivedMessageFromServer));
            client.Start();

            NetOutgoingMessage hail = client.CreateMessage("This is the hail message");
            client.Connect("127.0.0.1", 80, hail);

            form.Output("Connecting");
        }

        public void ReceivedMessageFromServer(object peer) {
            NetIncomingMessage im;
            while ((im = client.ReadMessage()) != null) {
                // handle incoming message
                switch (im.MessageType) {
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.ErrorMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.VerboseDebugMessage:
                        string text = im.ReadString();
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        NetConnectionStatus status = (NetConnectionStatus)im.ReadByte();

                        if (status == NetConnectionStatus.Connected)
                            form.Output("Connected");
                        else
                            form.Output("Disconnected");

                        string reason = im.ReadString();
                        form.Output(status.ToString() + ": " + reason);

                        break;
                    case NetIncomingMessageType.Data:
                        break;
                    default:
                        form.Output("Unhandled type: " + im.MessageType + " " + im.LengthBytes + " bytes");
                        break;
                }
                client.Recycle(im);
            }
        }

        public void Disconnect() {
            client.Disconnect("bye");
        }
    }
}
