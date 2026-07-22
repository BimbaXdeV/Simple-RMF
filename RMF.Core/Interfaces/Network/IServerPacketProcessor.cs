using RMF.Core.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Interfaces.Network
{
    public interface IServerPacketProcessor
    {
        Task SwitchHandle(Packet packet, IPEndPoint endPoint);
        void SearchHandle(Packet packet, string endPoint);
    }
}
