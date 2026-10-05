using RMF.Core.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Network
{
    public interface IServerPacketProcessor
    {
        Task SwitchHandle(IPacket packet, IPEndPoint endPoint);
        void SearchHandle(IPacket packet, IPEndPoint endPoint);
    }
}
