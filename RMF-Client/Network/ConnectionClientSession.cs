using RMF.Core.Events;
using RMF.Core.Interfaces;
using RMF.Core.Network;
using RMF.Core.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RMF_Client.Network
{
    internal class ConnectionClientSession : ClientSession, IConnectionClientSession
    {
        public DateTime ConnectedTime { get; private set; }

        public ConnectionClientSession(
            INetworkConnection connection,
            IProtocolReader reader,
            IPacketSender packetSender,
            IEventFactory eventFactory,
            int channelCapacity = 0,
            bool collectingStats = false,
            CancellationToken token = default
        ) : base(connection, reader, packetSender, eventFactory, channelCapacity, collectingStats, token)
        {
            this.ConnectedTime = DateTime.UtcNow;
        }

        public bool IsEventRunning(string eventName)
        {
            return this.Events.IsRunning(eventName);
        }

        public void StopEvent(string eventName)
        {
            this.Events.StopEvent(eventName);
        }

        public void StopAllEvents()
        {
            this.Events.StopAllRunning();
        }
    }
}
