using RMF.Core.Packets;
using System;
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
            MemoryStream ms = NetworkBuffer.GetMemoryStream();
            BinaryWriter writer = NetworkBuffer.GetBinaryWriter();

            try
            {
                packet.WriteToStream(writer);
                //ReadOnlyMemory<byte> payload = ms.GetBuffer().AsMemory(0, ms.Length);
                if (ms.TryGetBuffer(out ArraySegment<byte> buffer))
                {
                    await stream.WriteAsync(buffer.AsMemory(), token);
                }
            }
            catch (Exception)
            {
                throw ;
            }
            finally
            {
                ms.SetLength(0);
            }
        }
    }
}
