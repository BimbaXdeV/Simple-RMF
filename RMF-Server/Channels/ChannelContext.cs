using RMF_Server.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RMF_Server.Channels
{
    public class ChannelContext
    {
        public Channel<PacketContext> Channel { get; }
        public Task Worker { get; }
        public CancellationTokenSource TokenSource { get; }

        public ChannelContext(Channel<PacketContext> channel, Task worker, CancellationTokenSource cts)
        {
            this.Channel = channel;
            this.Worker = worker;
            this.TokenSource = cts;
        }
    }
}
