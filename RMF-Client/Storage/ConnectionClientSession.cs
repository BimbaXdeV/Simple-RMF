using RMF.Core.Bases;
using RMF.Core.Events;
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
    internal class ConnectionClientSession : ClientSession
    {
        public DateTime ConnectedTime { get; private set; }

        public ConnectionClientSession(
            TcpClient client,
            Stream? networkStream = null,
            int channelCapacity = 0,
            bool collectingStats = false,
            CancellationToken token = default
        ) : base(client, networkStream, channelCapacity, collectingStats, token)
        {
            this.ConnectedTime = DateTime.UtcNow;
        }
    }
}
