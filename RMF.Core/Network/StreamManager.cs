using RMF.Core.Bases;
using RMF.Core.Packets;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Network
{
    public static class StreamManager
    {
        public static async Task SendPacketAsync(Stream stream, Packet packet, CancellationToken token)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(PacketConfigurations.MaxPacketLengthKB);

            try
            {
                using MemoryStream ms = new(buffer);
                using BinaryWriter writer = new(ms);

                packet.WriteToStream(writer);
                //ReadOnlyMemory<byte> packedBuffer = ms.GetBuffer().AsMemory(0, (int)ms.Length);
                await stream.WriteAsync(buffer.AsMemory(0, (int)ms.Length), token);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
