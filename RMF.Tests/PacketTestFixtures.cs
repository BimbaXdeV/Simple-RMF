using RMF.Core.Packets.Client;
using RMF.Core.Packets.Server;
using RMF.Core.Packets;
using RMF.Core.Screen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RMF.Tests
{
    public static class PacketTestFixtures
    {
        private const long _defaultTimestamp = 1954600;

        public static readonly Dictionary<Type, Action<Packet, Packet>> SpecialDeserializeAsserters = new()
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

        private static ScreenPatch[] GenerateDirtyRects(
            short patchesCount,
            short targetMonitorWidth,
            short targetMonitorHeight,
            short minPatchWidth,
            short maxPatchWidth,
            short minPatchHeight,
            short maxPatchHeight
        )
        {
            Random random = new();
            ScreenPatch[] dirtyRects = new ScreenPatch[patchesCount];

            for (int i = 0; i < patchesCount; i++)
            {
                short x = (short)random.Next(0, targetMonitorWidth - maxPatchWidth);
                short y = (short)random.Next(0, targetMonitorHeight - maxPatchHeight);

                short width = (short)random.Next(minPatchWidth, maxPatchWidth);
                short height = (short)random.Next(minPatchHeight, maxPatchHeight);

                int dataLength = width * height;
                byte[] rectData = new byte[dataLength];
                Array.Fill(rectData, (byte)0xFF);

                dirtyRects[i] = new ScreenPatch(rectData, dataLength, x, y, width, height);
            }
            return dirtyRects;
        }

        public static byte[] BuildPayload(Action<BinaryWriter> writeAction)
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms);
            writeAction(writer);
            return ms.ToArray();
        }

        public static TheoryData<Type, byte[], Packet> GetDeserializationDataset()
        {
            TheoryData<Type, byte[], Packet> data = [];

            // Tiny/medium sized packets
            data.Add(
                typeof(HeartbeatPacket),
                BuildPayload(w =>
                {
                    w.Write(_defaultTimestamp);
                }),
                new HeartbeatPacket() { TurnedTimestamp = _defaultTimestamp }
            );

            string testMachineName = "LIN-1395350";
            string testAdminUsername = "Guest";
            string testOsName = "Lindows 35 v0.0113";
            string testCpuName = "Intel Pentium Super";
            string testCpuArc = "16";
            string testGpuName = "GeForce RTX 6080 ti"; // Why do I exist...
            long testRamCap = 1048576; // 1 MB of RAM is quite enough, you can run Linux on it 1 MB of RAM is quite enough, you can run Linux on it
            long testVramCap = 17179869184;
            data.Add(
                typeof(ClientInfoPacket),
                BuildPayload(w =>
                {
                    w.Write(testMachineName);
                    w.Write(testAdminUsername);
                    w.Write(testOsName);
                    w.Write(testCpuName);
                    w.Write(testCpuArc);
                    w.Write(testGpuName);
                    w.Write(testRamCap);
                    w.Write(testVramCap);
                }),
                new ClientInfoPacket()
                {
                    MachineName = testMachineName,
                    Username = testAdminUsername,
                    OSName = testOsName,
                    CPUName = testCpuName,
                    CPUArchitecture = testCpuArc,
                    GPUName = testGpuName,
                    RAMCapacity = testRamCap,
                    VRAMCapacity = testVramCap
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
                    w.Write(_defaultTimestamp);
                }),
                new PartingPacket()
                {
                    StatusCode = 54,
                    UptimeSecs = 180,
                    ReceivedPackets = -1,
                    SentPackets = -2,
                    LastTransferedTimestamp = _defaultTimestamp
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
                    w.Write(_defaultTimestamp);
                }),
                new ClientPingRequest()
                {
                    SendingTimestamp = _defaultTimestamp
                }
            );

            Guid testGuid = Guid.NewGuid();
            data.Add(
                typeof(HandshakePacket),
                BuildPayload(w =>
                {
                    w.Write(_defaultTimestamp);
                    w.Write(testGuid.ToByteArray());
                    w.Write("192.168.1.1");
                    w.Write(5678);
                    w.Write(-1);
                    w.Write(-1);
                }),
                new HandshakePacket()
                {
                    ConnectionTimestamp = _defaultTimestamp,
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

            short testDirtyFrameCount = 10;
            ScreenPatch[] dirtyRects = GenerateDirtyRects(testDirtyFrameCount, 1920, 1080, 50, 200, 50, 200);
            data.Add(
                typeof(StreamFramePacket),
                BuildPayload(w =>
                {
                    w.Write(false);
                    w.Write((byte)0);
                    w.Write(testDirtyFrameCount);

                    for (int i = 0; i < testDirtyFrameCount; i++)
                    {
                        w.Write(dirtyRects[i].X);
                        w.Write(dirtyRects[i].Y);
                        w.Write(dirtyRects[i].Width);
                        w.Write(dirtyRects[i].Height);
                        w.Write(dirtyRects[i].Length);

                        if (dirtyRects[i].Data.Length > 0)
                        {
                            w.Write(dirtyRects[i].Data);
                        }
                    }
                }),
                new StreamFramePacket()
                {
                    IsFullFrame = false,
                    FormatID = 0,
                    PatchesCount = testDirtyFrameCount,
                    Patches = dirtyRects
                }
            );

            return data;
        }

        public static TheoryData<Packet, byte[]> GetSerializationDataset()
        {
            TheoryData<Packet, byte[]> data = new();

            data.Add(
                new HeartbeatPacket()
                {
                    TurnedTimestamp = _defaultTimestamp
                },
                BuildPayload(w =>
                {
                    w.Write(_defaultTimestamp);
                })
            );

            string testMachineName = "Just machine";
            string testAdminUsername = "BimbaXdeV";
            string testOsName = "Windows 11 Pro";
            string testCpuName = "Intel Core i5 10400f";
            string testCpuArc = "64";
            string testGpuName = "GeForce RTX 3050";
            long testRamCap = 17179869184;
            long testVramCap = 8589934592;
            data.Add(
                new ClientInfoPacket()
                {
                    MachineName = testMachineName,
                    Username = testAdminUsername,
                    OSName = testOsName,
                    CPUName = testCpuName,
                    CPUArchitecture = testCpuArc,
                    GPUName = testGpuName,
                    RAMCapacity = testRamCap,
                    VRAMCapacity = testVramCap
                },
                BuildPayload(w =>
                {
                    w.Write(testMachineName);
                    w.Write(testAdminUsername);
                    w.Write(testOsName);
                    w.Write(testCpuName);
                    w.Write(testCpuArc);
                    w.Write(testGpuName);
                    w.Write(testRamCap);
                    w.Write(testVramCap);
                })
            );

            data.Add(
                new ClientVersionPacket()
                {
                    AppMajorVersion = 1,
                    AppMinorVersion = 2,
                    AppBuildVersion = 3,
                    CoreMajorVersion = 4,
                    CoreMinorVersion = 5,
                    CoreBuildVersion = 6
                },
                BuildPayload(w =>
                {
                    w.Write((short)1);
                    w.Write((short)2);
                    w.Write((short)3);
                    w.Write((short)4);
                    w.Write((short)5);
                    w.Write((short)6);
                })
            );

            data.Add(
                new PartingPacket()
                {
                    StatusCode = 0,
                    UptimeSecs = 7200,
                    ReceivedPackets = 5800,
                    SentPackets = 900,
                    LastTransferedTimestamp = _defaultTimestamp
                },
                BuildPayload(w =>
                {
                    w.Write((byte)0);
                    w.Write(7200L);
                    w.Write(5800L);
                    w.Write(900L);
                    w.Write(_defaultTimestamp);
                })
            );

            string endOfStreamingMessage = "This test was certainly easier than the deserialization one...";
            data.Add(
                new EndOfStreamingPacket()
                {
                    Reason = endOfStreamingMessage
                },
                BuildPayload(w =>
                {
                    w.Write(endOfStreamingMessage);
                })
            );

            data.Add(
                new ClientPingRequest()
                {
                    SendingTimestamp = _defaultTimestamp
                },
                BuildPayload(w =>
                {
                    w.Write(_defaultTimestamp);
                })
            );

            Guid testGuid = Guid.NewGuid();
            data.Add(
                new HandshakePacket()
                {
                    ConnectionTimestamp = _defaultTimestamp,
                    SessionID = testGuid,
                    RemoteIP = "127.0.0.2",
                    RemotePort = 8000,
                    SendBufferSize = 5120,
                    ReceiveBufferSize = 5120
                },
                BuildPayload(w =>
                {
                    w.Write(_defaultTimestamp);
                    w.Write(testGuid.ToByteArray());
                    w.Write("127.0.0.2");
                    w.Write(8000);
                    w.Write(5120);
                    w.Write(5120);
                })
            );

            data.Add(
                new ScreenshotRequest()
                {
                    FormatID = 0,
                    QualityPercent = 10
                },
                BuildPayload(w =>
                {
                    w.Write((byte)0);
                    w.Write((byte)10);
                })
            );

            data.Add(
                new StreamingRequest()
                {
                    IsActive = true,
                    FormatID = 2,
                    Quality = 65,
                    FrameUpdateRate = 500,
                    TargetFPS = 165
                },
                BuildPayload(w =>
                {
                    w.Write(true);
                    w.Write((byte)2);
                    w.Write((byte)65);
                    w.Write(500);
                    w.Write((short)165);
                })
            );

            // High-load packets
            int screenWidth = 100;
            int screenHeight = 100;
            int screenLength = screenWidth * screenHeight;
            byte[] testScreen = new byte[screenLength];
            Array.Fill(testScreen, (byte)0xFF);
            data.Add(
                new DesktopFramePacket()
                {
                    FormatID = 2,
                    Width = screenWidth,
                    Height = screenHeight,
                    ImageLength = screenLength,
                    ImageData = testScreen
                },
                BuildPayload(w =>
                {
                    w.Write((byte)2);
                    w.Write(screenWidth);
                    w.Write(screenHeight);
                    w.Write(screenLength);
                    w.Write(testScreen);
                })
            );

            ScreenPatch testSingleFrame = new(testScreen, screenLength, 0, 0, (short)screenWidth, (short)screenHeight);
            data.Add(
                new StreamFramePacket()
                {
                    IsFullFrame = true,
                    FormatID = 2,
                    PatchesCount = 1,
                    Patches = [testSingleFrame]
                },
                BuildPayload(w =>
                {
                    w.Write(true);
                    w.Write((byte)2);
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
                })
            );

            short testDirtyFrameCount = 20;
            ScreenPatch[] dirtyRects = GenerateDirtyRects(testDirtyFrameCount, 2560, 1440, 30, 100, 30, 100);
            data.Add(
                new StreamFramePacket()
                {
                    IsFullFrame = false,
                    FormatID = 0,
                    PatchesCount = testDirtyFrameCount,
                    Patches = dirtyRects
                },
                BuildPayload(w =>
                {
                    w.Write(false);
                    w.Write((byte)0);
                    w.Write(testDirtyFrameCount);

                    for (int i = 0; i < testDirtyFrameCount; i++)
                    {
                        w.Write(dirtyRects[i].X);
                        w.Write(dirtyRects[i].Y);
                        w.Write(dirtyRects[i].Width);
                        w.Write(dirtyRects[i].Height);
                        w.Write(dirtyRects[i].Length);

                        if (dirtyRects[i].Data.Length > 0)
                        {
                            w.Write(dirtyRects[i].Data);
                        }
                    }
                })
            );

            return data;
        }
    }
}
