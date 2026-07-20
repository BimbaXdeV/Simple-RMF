using RMF.Core.Bases;
using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Network;
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
    internal class SessionManager : IServerSessionManager
    {
        private readonly IProtocolReader _protocolReader;
        private readonly IPacketSender _packetSender;
        private readonly ILoggingEngine? _logger;

        private readonly ConcurrentDictionary<Guid, IServerClientSession> _connections = [];
        private readonly ConcurrentDictionary<string, Guid> _endPointIndex = [];
        private readonly ConcurrentDictionary<IPAddress, int> _ipConnectionsCount = [];

        public bool ConnectionsExist => !this._connections.IsEmpty;
        public int TotalConnections => this._connections.Count;

        public SessionManager(IProtocolReader protocolReader, IPacketSender packetSender, ILoggingEngine? logger = null)
        {
            this._protocolReader = protocolReader;
            this._packetSender = packetSender;
            this._logger = logger;
        }

        public void BroadcastPacket(Packet packet, CancellationToken token)
        {
            int totalTransferedPackets = 0;
            foreach (IServerClientSession session in this._connections.Values)
            {
                try
                {
                    session.SendPacket(packet);
                    totalTransferedPackets++;
                }
                catch (Exception ex)
                {
                    this._logger?.Warning($"Failed to transfer {session.GetType().Name} to \"{session.RemoteEndPoint}\" : {ex.Message}");
                }
            }
        }

        public IServerClientSession? NewConnection(INetworkConnection connection, CancellationToken token)
        {
            Guid sessionId = Guid.NewGuid();
            ServerClientSession session = new(
                connection,
                this._protocolReader,
                this._packetSender,
                channelCapacity: ConfigurationManager.ChannelPacketsCapacity,
                collectingStats: ConfigurationManager.EnableCollectingSessionStats,
                token: token
            );
            if (this._connections.TryAdd(sessionId, session))
            {
                this._endPointIndex.AddOrUpdate(connection.RemoteEndPoint.ToString(), sessionId, (_, _) => sessionId);

                IPAddress sessionIP = connection.RemoteEndPoint.Address;
                this._ipConnectionsCount.AddOrUpdate(sessionIP, 1, (_, actualCount) => actualCount + 1);

                return session;
            }
            return null;
        }

        public bool GetClientSession(string endPoint, out IServerClientSession? session)
        {
            session = null;
            if (this._endPointIndex.TryGetValue(endPoint, out Guid sessionId) && sessionId != Guid.Empty &&
                this._connections.TryGetValue(sessionId, out session) && session != null)
            {
                return true;
            }
            return false;
        }

        public Guid? GetSessionID(string endPoint)
        {
            if (this._endPointIndex.TryGetValue(endPoint, out Guid sessionId) && sessionId != Guid.Empty)
            {
                return sessionId;
            }
            return null;
        }

        public IServerClientSession[] GetActiveConnections()
        {
            return this._connections.Values.ToArray();
        }

        public int GetConnectionsFromIP(IPAddress ip)
        {
            return this._ipConnectionsCount.TryGetValue(ip, out int count) ? count : 0;
        }

        public void Disconnect(string endPoint)
        {
            if (!string.IsNullOrEmpty(endPoint) &&
                this._endPointIndex.TryGetValue(endPoint, out Guid sessionId) && sessionId != Guid.Empty &&
                this._connections.TryGetValue(sessionId, out IServerClientSession? session) && session != null)
            {
                IPAddress sessionIP = session.RemoteEndPoint.Address;
                int newCount = this._ipConnectionsCount.AddOrUpdate(sessionIP, 0, (_, actualCount) => actualCount - 1);

                if (newCount <= 0)
                {
                    this._ipConnectionsCount.TryRemove(sessionIP, out _);
                }

                session.StopProcessing();
                this._connections.TryRemove(sessionId, out _);
                this._endPointIndex.TryRemove(endPoint, out _);

                AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {this._connections.Count}");
                this._logger?.Output($"Client {endPoint} was disconnected");
            }
        }

        public void ClearConnections()
        {
            int disconnectedClientsCount = 0;
            int totalConnectedClients = this._connections.Count;

            foreach (KeyValuePair<Guid, IServerClientSession> entry in this._connections)
            {
                Disconnect(entry.Value.RemoteEndPoint.ToString());
                disconnectedClientsCount++;
            }
            this._connections.Clear();
            AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Online: {this._connections.Count}");
            this._logger?.Output($"Cleanup finished, disconnected {disconnectedClientsCount} / {totalConnectedClients}");
        }
    }
}
