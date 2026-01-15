using RMF_Server.Debugger;
using RMF_Server.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal static class SessionManager
    {
        private static readonly ConcurrentDictionary<string, ClientSession> Connections = [];
        public static int SessionsConnected() => Connections.Count;

        public static bool NewConnection(TcpClient client, string endPoint)
        {
            ClientSession session = new ClientSession(client, endPoint);
            return Connections.TryAdd(endPoint, session);
        }

        public static void Disconnect(TcpClient client, string endPoint)
        {
            if (Connections.TryRemove(endPoint, out _))
            {
                client.Close();
                AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {SessionsConnected()}");
                Logging.Output($"Client {endPoint} was disconnected");
            }
        }

        public static void ClearConnections()
        {
            Logging.Output("Connections are being cleared...");

            int disconnectedClientsCount = 0;
            int totalConnectedClients = SessionManager.SessionsConnected();

            foreach (var entry in Connections)
            {
                entry.Value.Client.Close();
                Logging.Output($"Client {entry.Key} was forced disconnected");
                disconnectedClientsCount++;
            }
            Connections.Clear();
            AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {SessionsConnected()}");
            Logging.Output($"Cleanup finished, disconnected {disconnectedClientsCount} / {totalConnectedClients}");
        }
    }
}
