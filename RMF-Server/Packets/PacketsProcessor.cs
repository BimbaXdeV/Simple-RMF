using RMF.Core.Packets;
using RMF.Core.Packets.Client;
using RMF.Core.Screen;
using RMF_Server.Debugger;
using RMF_Server.Logic;
using RMF_Server.Storage;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Packets
{
    internal static class PacketsProcessor
    {
        // Manual method, but lightning fast to execute
        public static async Task SwitchHandle(Packet packet, IPEndPoint endPoint)
        {
            switch (packet)
            {
                case HeartbeatPacket heartbeatPacket:
                    ProcessHeartbeatPacket(heartbeatPacket, endPoint);
                    break;

                case ClientVersionPacket clientVersionPacket:
                    ProcessClientVersionPacket(clientVersionPacket, endPoint);
                    break;

                case ClientInfoPacket clientInfoPacket:
                    ProcessClientInfoPacket(clientInfoPacket, endPoint);
                    break;

                case DesktopFramePacket desktopFramePacket:
                    await ProcessDesktopFramePacket(desktopFramePacket, endPoint);
                    break;

                case StreamFramePacket streamFramePacket:
                    ProcessStreamFramePacket(streamFramePacket, endPoint);
                    break;

                case PartingPacket partingPacket:
                    ProcessPartingPacket(partingPacket, endPoint);
                    break;
            }
        }

        // This handle method is too slow for streaming production, but it's here if you need it for scaling purposes
        //public static void SearchHandle(Packet packet, string endPoint)
        //{
        //    Type packetType = packet.GetType();
        //    var method = typeof(PacketsProcessor).GetMethod("Process" + packetType.Name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        //    if (method != null)
        //    {
        //        method.Invoke(null, new object[] { packet, endPoint });
        //    }
        //}

        private static void ProcessHeartbeatPacket(HeartbeatPacket packet, IPEndPoint endPoint)
        {
            double delay = (DateTime.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(packet.TurnedTimestamp)).TotalMilliseconds;
            Logging.Message($"Received heartbeat from {endPoint} : {delay}ms delay");
        }

        private static void ProcessClientVersionPacket(ClientVersionPacket packet, IPEndPoint endPoint)
        {
            if (SessionManager.Connections.TryGetValue(endPoint.ToString(), out ServerClientSession? session))
            {
                if (RMFVersion.Core?.Major != packet.CoreMajorVersion ||
                    RMFVersion.Core?.Minor != packet.CoreMinorVersion ||
                    RMFVersion.Core?.Build != packet.CoreBuildVersion)
                {
                    string clientCoreVersion = $"{packet.CoreMajorVersion}.{packet.CoreMinorVersion}.{packet.CoreBuildVersion}";

                    Logging.Warning($"Client {endPoint} is running a different version of RMF.Core ({clientCoreVersion}), disconnecting...");
                    SessionManager.Disconnect(endPoint.ToString());
                    return;
                }

                if (RMFVersion.App?.Major != packet.AppMajorVersion ||
                    RMFVersion.App.Minor != packet.AppMinorVersion)
                {
                    string clientAppVersion = $"{packet.AppMajorVersion}.{packet.AppMinorVersion}.{packet.AppBuildVersion}";

                    Logging.Warning($"Client {endPoint} is running a different version of RMF.App ({clientAppVersion}), disconnecting...");
                    SessionManager.Disconnect(endPoint.ToString());
                    return;
                }

                if (RMFVersion.App?.Build != packet.AppBuildVersion)
                {
                    string clientAppVersion = $"{packet.AppMajorVersion}.{packet.AppMinorVersion}.{packet.AppBuildVersion}";
                    Logging.Warning($"The connected client has a different build version ({clientAppVersion}), be careful");
                }
            }
        }

        private static void ProcessClientInfoPacket(ClientInfoPacket packet, IPEndPoint endPoint)
        {
            double ramCaparityGB = packet.RAMCapacity / 1024.0 / 1024.0 / 1024.0;
            double vramCaparityGB = packet.VRAMCapacity / 1024.0 / 1024.0 / 1024.0;

            Logging.Message(
                "Info about " + endPoint + "\n" +
                "- Machine name: " + packet.MachineName + "\n" +
                "- Username:     " + packet.OSName + "\n" +
                "- CPU:          (" + packet.CPUArchitecture + ") " + packet.CPUName + "\n" +
                "- GPU:          " + packet.GPUName + "\n" +
                "- Memory:       RAM: " + Math.Round(ramCaparityGB, 2) + " GB, VRAM: " + Math.Round(vramCaparityGB, 2) + " GB"
            );
        }

        private static async Task ProcessDesktopFramePacket(DesktopFramePacket packet, IPEndPoint endPoint)
        {
            if (packet.ImageData == null)
            {
                Logging.Message($"Failed to save an empty screenshot from \"{endPoint}\"");
                return;
            }

            string savePath = Path.GetFullPath(PathManager.GetResolvedPath("DesktopScreenshots",
                                                                           fileName: "%endPoint%_%datetime%",
                                                                           fileFormat: Enum.GetName(typeof(ScreenFormats), packet.FormatID)?.ToLower(),
                                                                           endPoint: endPoint.Address.ToString(),
                                                                           UpdateCachedDate: true));

            try
            {
                string? directory = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(savePath, packet.ImageData.AsMemory(0, packet.ImageLength));
                Logging.Message($"Screenshot from {endPoint} successfully saved on path: \"{savePath}\"");
            }
            catch (Exception ex)
            {
                Logging.Error($"Failed to save screenshot: {ex.Message}");
            }
        }

        private static void ProcessStreamFramePacket(StreamFramePacket packet, IPEndPoint endPoint)
        {
            if (SessionManager.Connections.TryGetValue(endPoint.ToString(), out ServerClientSession? session))
            {
                IPEndPoint? actualStreamer = WindowManager.StreamingClientEndPoint;
                if (actualStreamer == null)
                {
                    WindowManager.StreamingClientEndPoint = endPoint;
                    Logging.Output($"Streaming session started with {endPoint}");
                }
                else if (session.EndPoint != WindowManager.StreamingClientEndPoint)
                {
                    Logging.Warning($"Received a streaming frame from \"{endPoint}\" while the streaming session is active with {WindowManager.StreamingClientEndPoint}, disconnecting...");
                    SessionManager.Disconnect(endPoint.ToString());
                    return;
                }

                if (packet.Patches == null || packet.PatchesCount == 0)
                {
                    Logging.Message($"Received an empty streaming frame from \"{endPoint}\", disconnecting...");
                    SessionManager.Disconnect(endPoint.ToString());
                    return;
                }

                WindowManager.UpdateBitmap(packet.Patches, packet.PatchesCount, packet.IsFullFrame);
            }
        }

        private static void ProcessPartingPacket(PartingPacket packet, IPEndPoint endPoint)
        {
            Logging.Output($"Received a parting packet from {endPoint} with status code {packet.StatusCode} ({Enum.GetName(typeof(PartingStatusCodes), packet.StatusCode)})");
            Logging.Message($"Total {endPoint} uptime: {TimeSpan.FromSeconds(packet.UptimeSecs).ToString(@"dd\.hh\:mm\:ss")} | received: {packet.ReceivedPackets} | sent: {packet.SentPackets}", leftOffset: Logging.LogHeaderLength);
            SessionManager.Disconnect(endPoint.ToString());
        }
    }
}
