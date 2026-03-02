using RMF.Core.Bases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RMF_Server.Storage
{
    internal class ServerClientSession : ClientSession
    {
        //public TcpClient Client { get; set; }
        //private Channel<Packet> OutboundChannel = Channel.CreateUnbounded<Packet>();
        //private ChannelWriter<Packet> Writer => this.OutboundChannel.Writer;
        //public EventController Events { get; } = new();

        //public string EndPoint { get; set; } = "";
        //public string IPAddress { get; set; } = "0.0.0.0";
        //public ushort Port { get; set; } = 0;

        public int PacketsCount { get; private set; }
        public DateTime HandleStartTime { get; private set; }
        public DateTime LastTransferTime { get; private set; }

        public byte[]? LastFrame { get; set; }
        public DateTime? LastUpdate { get; set; }

        public ServerClientSession(TcpClient client, CancellationToken token) : base(client, token) { }

        public bool IsRateLimitExceed(int maxRate)
        {
            DateTime currentTime = DateTime.UtcNow;
            this.LastTransferTime = currentTime;

            if ((currentTime - this.HandleStartTime).TotalSeconds >= 1.0f)
            {
                this.PacketsCount = 0;
                this.HandleStartTime = currentTime;
            }

            this.PacketsCount++;
            return this.PacketsCount > maxRate;
        }
    }
}
