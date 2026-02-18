using RMF.Core.Packets;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Network
{
    public static class PayloadReader
    {
        public static async Task<byte[]> ReadAsync(NetworkStream stream, int size)
        {
            long bytesLimit = PacketConfigurations.MaxPacketLengthKB * 1024;
            if (size > bytesLimit || size < 0)
            {
                throw new OverflowException("The payload size exceeds the allowed buffer limit");
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                await stream.ReadExactlyAsync(buffer.AsMemory(0, size));
                return buffer;
            }
            catch (Exception)
            {
                ArrayPool<byte>.Shared.Return(buffer);
                throw;
            }
            //int totalBytesRead = 0;
            //while (totalBytesRead < size)
            //{
            //    int bytesRead = await stream.ReadAsync(buffer, totalBytesRead, size - totalBytesRead);
            //    if (bytesRead == 0)
            //    {
            //        throw new Exception("Connection closed unexpectedly");
            //    }
            //    totalBytesRead += bytesRead;
            //}
            //return buffer;
        }
    }
}
