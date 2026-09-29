using RMF.Core.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RMF.Tests
{
    public class PacketSerializationTests
    {
        [Theory]
        [MemberData(nameof(PacketTestFixtures.GetSerializationDataset), MemberType = typeof(PacketTestFixtures))]
        public void Serialize_ToStream_ShouldMatchExactBytes(Packet packet, byte[] expectedBytes)
        {
            MemoryStream ms = new(expectedBytes.Length);
            BinaryWriter writer = new(ms);

            packet.WriteToStream(writer);
            writer.Flush();

            byte[] actualBytes = ms.ToArray();
            short actualId = BitConverter.ToInt16(actualBytes, 0);
            int actualLength = BitConverter.ToInt32(actualBytes, 2);

            Assert.Equal(packet.ID, actualId);
            Assert.Equal(expectedBytes.Length, actualLength);

            ReadOnlySpan<byte> actualPayload = actualBytes.Skip(6).ToArray().AsSpan();
            Assert.True(expectedBytes.AsSpan().SequenceEqual(actualPayload));
        }
    }
}
