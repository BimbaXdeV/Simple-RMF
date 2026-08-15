using Microsoft.Extensions.Logging;
using RMF.Core.Bases;
using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Logic;
using RMF.Core.Interfaces.Network;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF_Server.Configurations;
using RMF_Server.Debugger;
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
        private readonly IEventFactory _eventFactory;
        private readonly ILogger<SessionManager> _logger;
        private readonly ControllerConfig _controllerConfig;
        private readonly ChannelConfig _channelConfig;

        private readonly ConcurrentDictionary<Guid, IServerClientSession> _connections = [];
        private readonly ConcurrentDictionary<string, Guid> _endPointIndex = [];
        private readonly ConcurrentDictionary<IPAddress, int> _ipConnectionsCount = [];

        public bool ConnectionsExist => !this._connections.IsEmpty;
        public int TotalConnections => this._connections.Count;

        public event Action<int>? ConnectionCountChanged;

        public SessionManager(
            IProtocolReader protocolReader,
            IPacketSender packetSender,
            IEventFactory eventFactory,
            ILogger<SessionManager> logger,
            ControllerConfig controllerConfig,
            ChannelConfig channelConfig
        )
        {
            this._protocolReader = protocolReader;
            this._packetSender = packetSender;
            this._eventFactory = eventFactory;
            this._logger = logger;
            this._controllerConfig = controllerConfig;
            this._channelConfig = channelConfig;
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
                    this._logger.LogError("Failed to transfer {PacketName} to {EndPoint} : {Exception}", packet.GetType().Name, session.RemoteEndPoint, ex);
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
                this._eventFactory,
                channelCapacity: this._channelConfig?.ChannelPacketsCapacity ?? default,
                collectingStats: this._controllerConfig?.EnableCollectingSessionStats ?? false,
                token: token
            );
            if (this._connections.TryAdd(sessionId, session))
            {
                this._endPointIndex.AddOrUpdate(connection.RemoteEndPoint.ToString(), sessionId, (_, _) => sessionId);

                IPAddress sessionIP = connection.RemoteEndPoint.Address;
                this._ipConnectionsCount.AddOrUpdate(sessionIP, 1, (_, actualCount) => actualCount + 1);

                this.ConnectionCountChanged?.Invoke(this.TotalConnections);
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

        // By default, it is not possible to prevent the method from notifying about online status changes.
        // However, method ClearConnections() must not trigger an event for every disconnected client
        public void Disconnect(string endPoint)
        {
            DisconnectInternal(endPoint, true);
        }

        private void DisconnectInternal(string endPoint, bool notifyOfChanges)
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

                this._logger.LogInformation("Client {EndPoint} was disconnected", endPoint);

                if (notifyOfChanges)
                {
                    // There was a knock on the door 20 000 times... ClearConnections(), thought Stierlitz
                    this.ConnectionCountChanged?.Invoke(this.TotalConnections);
                }
            }
        }

        public void ClearConnections()
        {
            int disconnectedClientsCount = 0;
            int totalConnectedClients = this._connections.Count;

            foreach (KeyValuePair<Guid, IServerClientSession> entry in this._connections)
            {
                DisconnectInternal(entry.Value.RemoteEndPoint.ToString(), false);
                disconnectedClientsCount++;
            }
            this._connections.Clear();
            this._logger.LogInformation("Cleanup finished, disconnected {Disconnected} / {Total}", disconnectedClientsCount, totalConnectedClients);
            this.ConnectionCountChanged?.Invoke(this.TotalConnections);
        }
    }
}
