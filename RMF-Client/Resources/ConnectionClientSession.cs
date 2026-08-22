using RMF.Core.Bases;
using RMF.Core.Events;
using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Network;
using RMF.Core.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RMF_Client.Storage
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
    }
}
