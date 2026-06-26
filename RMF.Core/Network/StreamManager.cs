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
        [ThreadStatic]
        private static MemoryStream? CachedMemoryStream;

        public static MemoryStream GetCachedStream()
        {
            CachedMemoryStream ??= new MemoryStream(PacketConfigurations.MinPacketLengthKB * 1000);
            CachedMemoryStream.Position = 0;
            CachedMemoryStream.SetLength(0);
            return CachedMemoryStream;
        }

        public static async Task SendPacketAsync(Stream stream, Packet packet, CancellationToken token)
        {
            MemoryStream ms = GetCachedStream();
            using BinaryWriter writer = new(ms, Encoding.UTF8, leaveOpen: true);

            packet.WriteToStream(writer);

            byte[] buffer = ms.GetBuffer();
            int bufferLength = (int)ms.Length;
            await stream.WriteAsync(buffer.AsMemory(0, bufferLength), token);
        }
    }
}
