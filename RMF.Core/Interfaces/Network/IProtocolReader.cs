using RMF.Core.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Interfaces.Network
{
    public interface IProtocolReader
    {
        Task<PacketHeader> ReadHeaderAsync(Stream stream, CancellationToken token);
        Task<byte[]> ReadPayloadAsync(Stream stream, int length, CancellationToken token);
    }
}
