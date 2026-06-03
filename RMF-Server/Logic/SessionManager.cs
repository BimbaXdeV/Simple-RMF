using RMF.Core.Bases;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF_Server.Debugger;
using RMF_Server.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal static class SessionManager
    {
        public static readonly ConcurrentDictionary<string, ServerClientSession> Connections = [];

        public static void BroadcastPacket(Packet packet, CancellationToken token)
        {
            int totalTransferedPackets = 0;
            foreach (ClientSession session in Connections.Values)
            {
                try
                {
                    session.SendPacket(packet);
                    totalTransferedPackets++;
                }
                catch (Exception ex)
                {
                    Logging.Warning($"Failed to transfer {session.GetType().Name} to \"{session.EndPoint?.ToString()}\" : {ex.Message}");
                }
            }
        }

        public static ServerClientSession? NewConnection(TcpClient client, string endPoint, Stream stream, CancellationToken token)
        {
            ServerClientSession session = new(
                client,
                networkStream: stream,
                channelCapacity: ConfigurationManager.ChannelPacketsCapacity,
                collectingStats: ConfigurationManager.EnableCollectingSessionStats,
                token: token
            );
            if (Connections.TryAdd(endPoint, session))
            {
                return session;
            }
            return null;
        }

        public static int GetSessionID(string endPoint)
        {
            List<string> connectedEndPoints = Connections.Keys.ToList();
            return connectedEndPoints.IndexOf(endPoint) + 1;
        }

        public static void Disconnect(string endPoint)
        {
            if (!string.IsNullOrEmpty(endPoint) && Connections.TryGetValue(endPoint, out ServerClientSession? session) && session != null)
            {
                session.StopProcessing();
                Connections.TryRemove(endPoint, out _);

                AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {Connections.Count}");
                Logging.Output($"Client {endPoint} was disconnected");
            }
        }

        public static void ClearConnections()
        {
            int disconnectedClientsCount = 0;
            int totalConnectedClients = Connections.Count;

            foreach (var entry in Connections)
            {
                entry.Value.Client.Close();
                Logging.Output($"Client {entry.Key} was forced disconnected");
                disconnectedClientsCount++;
            }
            Connections.Clear();
            AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {Connections.Count}");
            Logging.Output($"Cleanup finished, disconnected {disconnectedClientsCount} / {totalConnectedClients}");
        }
    }
}
