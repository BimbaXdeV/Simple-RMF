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
    public class PacketTests
    {
        private static readonly Dictionary<Type, Action<Packet, Packet>> _specialAsserters = new()
        {
            { 
                typeof(DesktopFramePacket),
                (expected, actual) =>
                {
                    DesktopFramePacket exp = (DesktopFramePacket)expected;
                    DesktopFramePacket act = (DesktopFramePacket)actual;

                    Assert.Equal(exp.FormatID, act.FormatID);
                    Assert.Equal(exp.Width, act.Width);
                    Assert.Equal(exp.Height, act.Height);
                    Assert.True(act.ImageData.AsSpan(0, act.ImageLength).SequenceEqual(exp.ImageData.AsSpan(0, exp.ImageLength)));
                }
            },
            {
                typeof(StreamFramePacket),
                (expected, actual) =>
                {
                    StreamFramePacket exp = (StreamFramePacket)expected;
                    StreamFramePacket act = (StreamFramePacket)actual;

                    Assert.Equal(exp.IsFullFrame, act.IsFullFrame);
                    Assert.Equal(exp.FormatID, act.FormatID);
                    Assert.Equal(exp.PatchesCount, act.PatchesCount);

                    Assert.NotNull(exp.Patches);
                    Assert.NotNull(act.Patches);

                    for (int i = 0; i < exp.PatchesCount; i++)
                    {
                        ScreenPatch expPatch = exp.Patches[i];
                        ScreenPatch actPatch = act.Patches[i];
                        Assert.True(actPatch.Data.AsSpan(0, actPatch.Length).SequenceEqual(expPatch.Data.AsSpan(0, expPatch.Length)));
                    }
                }
            }
        };

        public static byte[] BuildPayload(Action<BinaryWriter> writeAction)
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms);
            writeAction(writer);
            return ms.ToArray();

        }

        public static TheoryData<Type, byte[], Packet> GetPacketDumps()
        {
            TheoryData<Type, byte[], Packet> data = new();
            
            long defaultTimestamp = 1954600;

            // Tiny/medium sized packets
            data.Add(
                typeof(HeartbeatPacket),
                BuildPayload(w =>
                {
                    w.Write(defaultTimestamp);
                }),
                new HeartbeatPacket() { TurnedTimestamp = defaultTimestamp }
            );

            data.Add(
                typeof(HeartbeatPacket),
                BuildPayload(w =>
                {
                    w.Write(defaultTimestamp);
                }),
                new HeartbeatPacket() { TurnedTimestamp = defaultTimestamp }
            );

            long defaultRamCapacity = 1048576; // 1 MB of RAM is quite enough, you can run Linux on it
            long defaultVRamCapacity = 17179869184;
            data.Add(
                typeof(ClientInfoPacket),
                BuildPayload(w =>
                {
                    w.Write("WIN-1395350");
                    w.Write("Admin");
                    w.Write("Lindows 35 v0.0113");
                    w.Write("Intel Pentium Super");
                    w.Write("16");
                    w.Write("GeForce RTX 5080 ti"); // Why do I exist...
                    w.Write(defaultRamCapacity);
                    w.Write(defaultVRamCapacity);
                }),
                new ClientInfoPacket()
                {
                    MachineName = "WIN-1395350",
                    Username = "Admin",
                    OSName = "Lindows 35 v0.0113",
                    CPUName = "Intel Pentium Super",
                    CPUArchitecture = "16",
                    GPUName = "GeForce RTX 5080 ti",
                    RAMCapacity = defaultRamCapacity,
                    VRAMCapacity = defaultVRamCapacity
                }
            );

            data.Add(
                typeof(ClientVersionPacket),
                BuildPayload(w =>
                {
                    w.Write((short)1);
                    w.Write((short)2);
                    w.Write((short)3);
                    w.Write((short)4);
                    w.Write((short)5);
                    w.Write((short)6);
                }),
                new ClientVersionPacket()
                {
                    AppMajorVersion = 1,
                    AppMinorVersion = 2,
                    AppBuildVersion = 3,
                    CoreMajorVersion = 4,
                    CoreMinorVersion = 5,
                    CoreBuildVersion = 6
                }
            );

            data.Add(
                typeof(PartingPacket),
                BuildPayload(w =>
                {
                    w.Write((byte)54);
                    w.Write(180L);
                    w.Write(-1L);
                    w.Write(-2L);
                    w.Write(defaultTimestamp);
                }),
                new PartingPacket()
                {
                    StatusCode = 54,
                    UptimeSecs = 180,
                    ReceivedPackets = -1,
                    SentPackets = -2,
                    LastTransferedTimestamp = defaultTimestamp
                }
            );

            string endOfStreamingMessage = "I see you`re sitting here looking over the tests? :)";
            data.Add(
                typeof(EndOfStreamingPacket),
                BuildPayload(w =>
                {
                    w.Write(endOfStreamingMessage);
                }),
                new EndOfStreamingPacket()
                {
                    Reason = endOfStreamingMessage
                }
            );

            data.Add(
                typeof(ClientPingRequest),
                BuildPayload(w =>
                {
                    w.Write(defaultTimestamp);
                }),
                new ClientPingRequest()
                {
                    SendingTimestamp = defaultTimestamp
                }
            );

            Guid testGuid = Guid.NewGuid();
            data.Add(
                typeof(HandshakePacket),
                BuildPayload(w =>
                {
                    w.Write(defaultTimestamp);
                    w.Write(testGuid.ToByteArray());
                    w.Write("192.168.1.1");
                    w.Write(5678);
                    w.Write(-1);
                    w.Write(-1);
                }),
                new HandshakePacket()
                {
                    ConnectionTimestamp = defaultTimestamp,
                    SessionID = testGuid,
                    RemoteIP = "192.168.1.1",
                    RemotePort = 5678,
                    SendBufferSize = -1,
                    ReceiveBufferSize = -1
                }
            );

            data.Add(
                typeof(ScreenshotRequest),
                BuildPayload(w =>
                {
                    w.Write((byte)2);
                    w.Write((byte)101);
                }),
                new ScreenshotRequest()
                {
                    FormatID = 2,
                    QualityPercent = 101
                }
            );

            data.Add(
                typeof(StreamingRequest),
                BuildPayload(w =>
                {
                    w.Write(true);
                    w.Write((byte)3);
                    w.Write((byte)50);
                    w.Write(-5);
                    w.Write((short)1);
                }),
                new StreamingRequest()
                {
                    IsActive = true,
                    FormatID = 3,
                    Quality = 50,
                    FrameUpdateRate = -5,
                    TargetFPS = 1
                }
            );

            // High-load packets
            int screenWidth = 125;
            int screenHeight = 90;
            int screenLength = screenWidth * screenHeight;
            byte[] testScreen = new byte[screenLength];
            Array.Fill(testScreen, (byte)0xFF);
            data.Add(
                typeof(DesktopFramePacket),
                BuildPayload(w =>
                {
                    w.Write((byte)1);
                    w.Write(screenWidth);
                    w.Write(screenHeight);
                    w.Write(screenLength);
                    w.Write(testScreen);
                }),
                new DesktopFramePacket()
                {
                    FormatID = 1,
                    Width = screenWidth,
                    Height = screenHeight,
                    ImageLength = screenLength,
                    ImageData = testScreen
                }
            );

            ScreenPatch testSingleFrame = new(testScreen, screenLength, 0, 0, (short)screenWidth, (short)screenHeight);
            data.Add(
                typeof(StreamFramePacket),
                BuildPayload(w =>
                {
                    w.Write(true);
                    w.Write((byte)0);
                    w.Write((short)1);

                    w.Write(testSingleFrame.X);
                    w.Write(testSingleFrame.Y);
                    w.Write(testSingleFrame.Width);
                    w.Write(testSingleFrame.Height);
                    w.Write(testSingleFrame.Length);
                    
                    if (testSingleFrame.Data.Length > 0)
                    {
                        w.Write(testSingleFrame.Data, 0, testSingleFrame.Length);
                    }
                }),
                new StreamFramePacket()
                {
                    IsFullFrame = true,
                    FormatID = 0,
                    PatchesCount = 1,
                    Patches = [testSingleFrame]
                }
            );

            //data.Add(

            //);

            return data;
        }

        [Theory]
        [MemberData(nameof(GetPacketDumps))]
        public void Deserialize_FromPacketDumps_ShouldMatchExactState(Type packetType, byte[] networkBytearray, Packet expectedPacket)
        {
            SpanReader reader = new(networkBytearray);
            Packet packet = (Packet)Activator.CreateInstance(packetType)!;

            try
            {
                packet.Deserialize(ref reader);

                // Unfortunately, the standard verification system cannot handle complex package types
                // that use unmanaged memory, so dedicated asserters have been created specifically for them
                if (_specialAsserters.TryGetValue(packetType, out Action<Packet, Packet>? asserter))
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
