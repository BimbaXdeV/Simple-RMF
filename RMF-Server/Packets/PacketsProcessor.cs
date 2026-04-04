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

                case SystemInfoPacket systemInfoPacket:
                    ProcessSystemInfoPacket(systemInfoPacket, endPoint);
                    break;

                case DesktopFramePacket desktopFramePacket:
                    await ProcessDesktopFramePacket(desktopFramePacket, endPoint);
                    break;

                case StreamFramePacket streamFramePacket:
                    ProcessStreamFramePacket(streamFramePacket, endPoint);
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
            double delay = (DateTime.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(packet.Timestamp)).TotalMilliseconds;
            Logging.Message($"Received heartbeat from {endPoint} : {delay}ms delay");
        }

        private static void ProcessSystemInfoPacket(SystemInfoPacket packet, IPEndPoint endPoint)
        {
            Logging.Message($"Info about {endPoint} - Name: {packet.MachineName}, User: {packet.Username}, OS: {packet.OS}, Architecture: {packet.Architecture}");
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
                session.LastFrameUpdate = DateTime.Now;
                if (packet.ImageData == null)
                {
                    Logging.Message($"Received an empty streaming frame from \"{endPoint}\", disconnecting...");
                    SessionManager.Disconnect(endPoint.ToString());
                    return;
                }
                WindowManager.UpdateFrame(packet.ImageData, packet.Width, packet.Height);
                ArrayPool<byte>.Shared.Return(packet.ImageData);
            }
        }
    }
}
