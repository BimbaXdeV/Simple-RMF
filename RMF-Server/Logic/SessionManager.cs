using RMF.Core.Network;
using RMF.Core.Packets;
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
        public static readonly ConcurrentDictionary<string, ClientSession> Connections = [];

        public static async Task SendPacketAsync(string endPoint, Packet packet, CancellationToken token)
        {
            if (Connections.TryGetValue(endPoint, out ClientSession? session) && session?.Client.Connected == true)
            {
                try
                {
                    await StreamManager.SendPacketAsync(session.Client.GetStream(), packet, token);
                }
                catch (Exception ex)
                {
                    Logging.Error($"Failed to send packet to client {endPoint}: {ex.Message}");
                }
            }
        }

        public static async Task BroadcastPacket(Packet packet, CancellationToken token)
        {
            Task[] tasks = Connections.Values.Select(session => SendPacketAsync(session.EndPoint, packet, token)).ToArray();
            await Task.WhenAll(tasks);
        }

        public static bool NewConnection(TcpClient client, string endPoint)
        {
            ClientSession session = new(client);
            return Connections.TryAdd(endPoint, session);
        }

        public static int GetSessionID(string endPoint)
        {
            List<string> connectedEndPoints = Connections.Keys.ToList();
            return connectedEndPoints.IndexOf(endPoint);
        }

        public static void Disconnect(TcpClient client, string endPoint)
        {
            if (Connections.TryGetValue(endPoint, out ClientSession? session) && session != null)
            {
                session.Events.StopAllRunning();
                client.Close();
                Connections.TryRemove(endPoint, out _);

                AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {Connections.Count}");
                Logging.Output($"Client {endPoint} was disconnected");
            }
        }

        public static void ClearConnections()
        {
            Logging.Output("Connections are being cleared...");

            int disconnectedClientsCount = 0;
            int totalConnectedClients = Connections.Count();

            foreach (var entry in Connections)
            {
                entry.Value.Client.Close();
                Logging.Output($"Client {entry.Key} was forced disconnected");
                disconnectedClientsCount++;
            }
            Connections.Clear();
            AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {Connections.Count()}");
            Logging.Output($"Cleanup finished, disconnected {disconnectedClientsCount} / {totalConnectedClients}");
        }
    }
}
