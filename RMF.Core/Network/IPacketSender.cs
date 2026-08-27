using RMF.Core.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Network
{
    public interface IPacketSender
    {
        MemoryStream GetCachedStream();
        Task SendPacketAsync(Stream stream, Packet packet, CancellationToken token);
    }
}
