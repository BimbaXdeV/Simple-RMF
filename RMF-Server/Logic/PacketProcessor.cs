using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Network;
using RMF.Core.Packets;
using RMF.Core.Packets.Client;
using RMF.Core.Screen;
using RMF_Server.Configurations;
using RMF_Server.Debugger;
using RMF_Server.Storage;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal class PacketProcessor : IServerPacketProcessor
    {
        private readonly IAvaloniaManager _avaloniaManager;
        private readonly IServerSessionManager _sessionManager;
        private readonly ILoggingEngine? _logger;
        private readonly AppearanceConfig? _appearanceConfig;

        public PacketProcessor(
            IAvaloniaManager avaloniaManager,
            IServerSessionManager sessionManager,
            ILoggingEngine? logger = null,
            AppearanceConfig? appearanceConfig = null
        )
        {
            _avaloniaManager = avaloniaManager;
            _sessionManager = sessionManager;
            _logger = logger;
            _appearanceConfig = appearanceConfig;
        }

        // Manual method, but lightning fast to execute
        public async Task SwitchHandle(Packet packet, IPEndPoint endPoint)
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

                case EndOfStreamingPacket endOfStreamingPacket:
                    await ProcessEndOfStreamingPacket(endOfStreamingPacket, endPoint);
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
        public void SearchHandle(Packet packet, string endPoint)
        {
            MethodInfo? method = typeof(PacketProcessor).GetMethod("Process" + packet.GetType().Name, BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(null, [packet, endPoint]);
        }

        private void ProcessHeartbeatPacket(HeartbeatPacket packet, IPEndPoint endPoint)
        {
            double delay = (DateTime.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(packet.TurnedTimestamp)).TotalMilliseconds;
            _logger?.Message($"Received heartbeat from {endPoint} : {delay}ms delay");
        }

        private void ProcessClientVersionPacket(ClientVersionPacket packet, IPEndPoint endPoint)
        {
            if (_sessionManager.GetClientSession(endPoint.ToString(), out _))
            {
                if (RMFVersion.Core?.Major != packet.CoreMajorVersion ||
                    RMFVersion.Core?.Minor != packet.CoreMinorVersion ||
                    RMFVersion.Core?.Build != packet.CoreBuildVersion)
                {
                    string clientCoreVersion = $"{packet.CoreMajorVersion}.{packet.CoreMinorVersion}.{packet.CoreBuildVersion}";

                    _logger?.Warning($"Client {endPoint} is running a different version of RMF.Core ({clientCoreVersion}), disconnecting...");
                    _sessionManager.Disconnect(endPoint.ToString());
                    return;
                }

                if (RMFVersion.App?.Major != packet.AppMajorVersion ||
                    RMFVersion.App.Minor != packet.AppMinorVersion)
                {
                    string clientAppVersion = $"{packet.AppMajorVersion}.{packet.AppMinorVersion}.{packet.AppBuildVersion}";

                    _logger?.Warning($"Client {endPoint} is running a different version of RMF.App ({clientAppVersion}), disconnecting...");
                    _sessionManager.Disconnect(endPoint.ToString());
                    return;
                }

                if (RMFVersion.App?.Build != packet.AppBuildVersion)
                {
                    string clientAppVersion = $"{packet.AppMajorVersion}.{packet.AppMinorVersion}.{packet.AppBuildVersion}";
                    _logger?.Warning($"The connected client has a different build version ({clientAppVersion}), be careful");
                }
            }
        }

        private void ProcessClientInfoPacket(ClientInfoPacket packet, IPEndPoint endPoint)
        {
            double ramCaparityGB = packet.RAMCapacity / 1024.0 / 1024.0 / 1024.0;
            double vramCaparityGB = packet.VRAMCapacity / 1024.0 / 1024.0 / 1024.0;

            _logger?.Message(
                "Info about " + endPoint + Environment.NewLine +
                "- Machine name: " + packet.MachineName + Environment.NewLine +
                "- Username:     " + packet.OSName + Environment.NewLine +
                "- CPU:          (" + packet.CPUArchitecture + ") " + packet.CPUName + Environment.NewLine +
                "- GPU:          " + packet.GPUName + Environment.NewLine +
                "- Memory:       RAM: " + Math.Round(ramCaparityGB, 2) + " GB, VRAM: " + Math.Round(vramCaparityGB, 2) + " GB"
            );
        }

        private async Task ProcessEndOfStreamingPacket(EndOfStreamingPacket packet, IPEndPoint endPoint)
        {
            if (_sessionManager.GetClientSession(endPoint.ToString(), out IServerClientSession? session))
            {
                if (session!.RemoteEndPoint == _avaloniaManager.StreamingClientEndPoint)
                {
                    try
                    {
                        _avaloniaManager.SetWindowTitle(_appearanceConfig?.WindowTitle ?? string.Empty);
                        await _avaloniaManager.HideWindow();
                    }
                    finally
                    {
                        _avaloniaManager.StreamingClientEndPoint = null;
                        string breakReason = !string.IsNullOrEmpty(packet.Reason) ? "Reason: " + packet.Reason : string.Empty;
                        _logger?.Output($"Streaming session ended with {endPoint}{breakReason}");
                    }
                }
                else
                {
                    _logger?.Warning($"Received an end of streaming packet from \"{endPoint}\" while the streaming session is active with {_avaloniaManager.StreamingClientEndPoint}, disconnecting...");
                    _sessionManager.Disconnect(endPoint.ToString());
                }
            }
        }

        private async Task ProcessDesktopFramePacket(DesktopFramePacket packet, IPEndPoint endPoint)
        {
            if (packet.ImageData == null)
            {
                _logger?.Message($"Failed to save an empty screenshot from \"{endPoint}\"");
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
                _logger?.Message($"Screenshot from {endPoint} successfully saved on path: \"{savePath}\"");
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to save screenshot: {ex.Message}");
            }
        }

        private void ProcessStreamFramePacket(StreamFramePacket packet, IPEndPoint endPoint)
        {
            if (_sessionManager.GetClientSession(endPoint.ToString(), out IServerClientSession? session))
            {
                IPEndPoint? actualStreamer = _avaloniaManager.StreamingClientEndPoint;
                if (actualStreamer == null)
                {
                    _logger?.Warning($"Received a streaming frame from \"{endPoint}\" while no streaming session is active, nothing to do");
                    return;
                }

                if (session!.RemoteEndPoint != actualStreamer)
                {
                    _logger?.Warning($"Received a streaming frame from \"{endPoint}\" while the streaming session is active with {_avaloniaManager.StreamingClientEndPoint}, disconnecting...");
                    _sessionManager.Disconnect(endPoint.ToString());
                    return;
                }

                if (packet.Patches == null || packet.PatchesCount == 0)
                {
                    _logger?.Message($"Received an empty streaming frame from \"{endPoint}\", disconnecting...");
                    _sessionManager.Disconnect(endPoint.ToString());
                    return;
                }
                _avaloniaManager.UpdateBitmap(packet.Patches, packet.PatchesCount, packet.IsFullFrame);
            }
        }

        private void ProcessPartingPacket(PartingPacket packet, IPEndPoint endPoint)
        {
            _logger?.Output($"Received a parting packet from {endPoint} with status code {packet.StatusCode} ({Enum.GetName(typeof(PartingStatusCodes), packet.StatusCode)})");
            _logger?.Message($"Total {endPoint} uptime: {TimeSpan.FromSeconds(packet.UptimeSecs).ToString(@"dd\.hh\:mm\:ss")} | received: {packet.ReceivedPackets} | sent: {packet.SentPackets}", leftOffset: _logger?.LogHeaderLength ?? 0);
            _sessionManager.Disconnect(endPoint.ToString());
        }
    }
}
