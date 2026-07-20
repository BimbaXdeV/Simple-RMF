using RMF.Core.Interfaces.Network;
using RMF.Core.Packets;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Network
{
    public class ProtocolReader : IProtocolReader
    {
        private readonly long _bytesLimit;

        public ProtocolReader(long bytesLimit)
        {
            this._bytesLimit = bytesLimit;
        }

        private void ValidateStreamLength(int length)
        {
            if (length > this._bytesLimit || length < 0)
            {
                throw new OverflowException("The payload size exceeds the allowed buffer limit");
            }
        }

        public async Task<PacketHeader> ReadHeaderAsync(Stream stream, CancellationToken token)
        {
            int headerLength = PacketHeader.Size;
            ValidateStreamLength(headerLength);

            byte[] headerBuffer = new byte[headerLength];
            await stream.ReadExactlyAsync(headerBuffer, token);
            return new PacketHeader(
                // Packet ID     :  (0, 1)       bytes
                BinaryPrimitives.ReadInt16LittleEndian(headerBuffer.AsSpan(0, sizeof(short))),
                // Payload length:  (2, 3, 4, 5) bytes
                BinaryPrimitives.ReadInt32LittleEndian(headerBuffer.AsSpan(sizeof(short), sizeof(int)))
            );
        }

        public async Task<byte[]> ReadPayloadAsync(Stream stream, int length, CancellationToken token)
        {
            ValidateStreamLength(length);

            byte[] buffer = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                await stream.ReadExactlyAsync(buffer.AsMemory(0, length), token);
                return buffer;
            }
            catch (Exception)
            {
                ArrayPool<byte>.Shared.Return(buffer);
                throw;
            }
        }
    }
}
