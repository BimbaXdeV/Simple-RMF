using RMF.Core.Bases;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF_Server.Debugger;
using RMF_Server.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal static class SessionManager
    {
        private static readonly ConcurrentDictionary<Guid, ServerClientSession> Connections = [];
        private static readonly ConcurrentDictionary<string, Guid> EndPointIndex = [];
        private static readonly ConcurrentDictionary<IPAddress, int> IPConnectionsCount = [];

        public static bool ConnectionsExist => !Connections.IsEmpty;
        public static int TotalConnections => Connections.Count;

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
            Guid sessionId = Guid.NewGuid();
            ServerClientSession session = new(
                client,
                networkStream: stream,
                channelCapacity: ConfigurationManager.ChannelPacketsCapacity,
                collectingStats: ConfigurationManager.EnableCollectingSessionStats,
                token: token
            );
            if (Connections.TryAdd(sessionId, session))
            {
                EndPointIndex.AddOrUpdate(endPoint, sessionId, (_, _) => sessionId);

                IPAddress sessionIP = ((IPEndPoint)client.Client.RemoteEndPoint!).Address;
                IPConnectionsCount.AddOrUpdate(sessionIP, 1, (_, actualCount) => actualCount + 1);

                return session;
            }
            return null;
        }

        public static bool GetClientSession(string endPoint, out ServerClientSession? session)
        {
            session = null;
            if (EndPointIndex.TryGetValue(endPoint, out Guid sessionId) && sessionId != Guid.Empty &&
                Connections.TryGetValue(sessionId, out session) && session != null)
            {
                return true;
            }
            return false;
        }

        public static Guid? GetSessionID(string endPoint)
        {
            if (EndPointIndex.TryGetValue(endPoint, out Guid sessionId) && sessionId != Guid.Empty)
            {
                return sessionId;
            }
            return null;
        }

        public static ServerClientSession[] GetActiveConnections()
        {
            return Connections.Values.ToArray();
        }

        public static int GetConnectionsFromIP(IPAddress ip)
        {
            return IPConnectionsCount.TryGetValue(ip, out int count) ? count : 0;
        }

        public static void Disconnect(string endPoint)
        {
            if (!string.IsNullOrEmpty(endPoint) &&
                EndPointIndex.TryGetValue(endPoint, out Guid sessionId) && sessionId != Guid.Empty &&
                Connections.TryGetValue(sessionId, out ServerClientSession? session) && session != null)
            {
                if (session.Client.Client.RemoteEndPoint is IPEndPoint sessionEndPoint)
                {
                    IPAddress sessionIP = sessionEndPoint.Address;
                    int newCount = IPConnectionsCount.AddOrUpdate(sessionIP, 0, (_, actualCount) => actualCount - 1);

                    if (newCount <= 0)
                    {
                        IPConnectionsCount.TryRemove(sessionIP, out _);
                    }
                }

                session.StopProcessing();
                Connections.TryRemove(sessionId, out _);
                EndPointIndex.TryRemove(endPoint, out _);

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
