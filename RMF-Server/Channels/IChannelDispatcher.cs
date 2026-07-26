using RMF.Core.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Channels
{
    public interface IChannelDispatcher
    {
        (int, int) StartFound();
        Task EnqueuePacketAsync(PacketContext context);
        Task CloseChannels();
        bool IsChannelExists(int key);
    }
}
