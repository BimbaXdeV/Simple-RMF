using RMF.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Storage
{
    internal static class ConnectionSession
    {
        public static TcpClient? Client { get; set; }
        public static EventController Events { get; private set; } = new();

        private static void CloseClient()
        {
            if (Client != null)
            {
                Client.Close();
                Client = null;
            }
        }

        public static bool IsConnected()
        {
            return Client != null && Client.Connected;
        }

        public static int GetRemotePort()
        {
            IPEndPoint? remoteEndpoint = Client?.Client.RemoteEndPoint as IPEndPoint;
            return remoteEndpoint?.Port ?? -1;
        }

        public static void NewSession(string ip, int port)
        {
            if (Client != null)
            {
                CloseClient();
            }
            Client = new TcpClient(ip, port);
            if (Events == null)
            {
                Events = new EventController();
            }
            else
            {
                // We don't need to rewrite the entire controller at once; it's enough to simply delete the remaining active tasks before a new session.
                Events.StopAllRunning();
            }
        }

        public static void Disconnect()
        {
            if (Client != null)
            {
                Events?.StopAllRunning();
                Client.Close();
                Client = null;
            }
        }
    }
}
