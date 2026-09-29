using RMF.Core.Interfaces;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF.Core.Packets.Client;
using RMF.Core.Packets.Server;
using RMF.Core.Screen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RMF.Tests
{
    public class PacketDeserializationTests
    {
        [Theory]
        [MemberData(nameof(PacketTestFixtures.GetDeserializationDataset), MemberType = typeof(PacketTestFixtures))]
        public void Deserialize_FromPacketDumps_ShouldMatchExactState(Type packetType, byte[] networkBytearray, Packet expectedPacket)
        {
            SpanReader reader = new(networkBytearray);
            Packet packet = (Packet)Activator.CreateInstance(packetType)!;

            try
            {
                packet.Deserialize(ref reader);

                // Unfortunately, the standard verification system cannot handle complex package types
                // that use unmanaged memory, so dedicated asserters have been created specifically for them
                if (PacketTestFixtures.SpecialDeserializeAsserters.TryGetValue(packetType, out Action<Packet, Packet>? asserter))
                {
                    asserter(expectedPacket, packet);
                }
                else
                {
                    Assert.Equivalent(expectedPacket, packet, strict: true);
                }
            }
            finally
            {
                if (packet is IReleasable releasable)
                {
                    releasable.Release();
                }
            }
        }
    }
}
