using Microsoft.Extensions.Logging;
using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Network;
using RMF.Core.Packets;
using RMF.Core.Packets.Client;
using RMF.Core.Screen;
using RMF_Server.Configurations;
using RMF_Server.Debugger;
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
        private readonly ILogger _logger;
        private readonly AppearanceConfig _appearanceConfig;
        private readonly StreamingConfig _streamingConfig;

        public PacketProcessor(
            IAvaloniaManager avaloniaManager,
            IServerSessionManager sessionManager,
            ILogger logger,
            AppearanceConfig appearanceConfig,
            StreamingConfig streamingConfig
        )
        {
            this._avaloniaManager = avaloniaManager;
            this._sessionManager = sessionManager;
            this._logger = logger;
            this._appearanceConfig = appearanceConfig;
            this._streamingConfig = streamingConfig;
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
            this._logger.Log(LogLevel.None, "Received heartbeat from {EndPoint} : {NetworkLatency} ms delay", endPoint, delay);
        }

        private void ProcessClientVersionPacket(ClientVersionPacket packet, IPEndPoint endPoint)
        {
            if (this._sessionManager.GetClientSession(endPoint.ToString(), out _))
            {
                if (RmfVersion.Core?.Major != packet.CoreMajorVersion ||
                    RmfVersion.Core?.Minor != packet.CoreMinorVersion ||
                    RmfVersion.Core?.Build != packet.CoreBuildVersion)
                {
                    string clientCoreVersion = $"{packet.CoreMajorVersion}.{packet.CoreMinorVersion}.{packet.CoreBuildVersion}";

                    this._logger.LogError("Client {EndPoint} is running a different version of RMF.Core ({ReceivedCoreVersion}), disconnecting...", endPoint, clientCoreVersion);
                    this._sessionManager.Disconnect(endPoint.ToString());
                    return;
                }

                string clientAppVersion = $"{packet.AppMajorVersion}.{packet.AppMinorVersion}.{packet.AppBuildVersion}";
                if (RmfVersion.App?.Major != packet.AppMajorVersion || RmfVersion.App.Minor != packet.AppMinorVersion)
                {
                    this._logger.LogError("Client {EndPoint} is running a different version of RMF.App ({ReceivedAppVersion}), disconnecting...", endPoint, clientAppVersion);
                    this._sessionManager.Disconnect(endPoint.ToString());
                    return;
                }

                if (RmfVersion.App?.Build != packet.AppBuildVersion)
                {
                    this._logger.LogWarning("The connected client has a different build version ({ReceivedAppVersion}), be careful", clientAppVersion);
                }
            }
        }

        private void ProcessClientInfoPacket(ClientInfoPacket packet, IPEndPoint endPoint)
        {
            double ramCaparityGB = packet.RAMCapacity / 1024.0 / 1024.0 / 1024.0;
            double vramCaparityGB = packet.VRAMCapacity / 1024.0 / 1024.0 / 1024.0;

            this._logger.Log(
                LogLevel.None,
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
            if (this._sessionManager.GetClientSession(endPoint.ToString(), out IServerClientSession? session))
            {
                if (session!.RemoteEndPoint == this._avaloniaManager.StreamingClientEndPoint)
                {
                    try
                    {
                        this._avaloniaManager.SetWindowTitle(_appearanceConfig?.WindowTitle ?? string.Empty);
                        await this._avaloniaManager.HideWindow();
                    }
                    finally
                    {
                        this._avaloniaManager.StreamingClientEndPoint = null;
                        string breakReason = !string.IsNullOrEmpty(packet.Reason) ? "Reason: " + packet.Reason : string.Empty;
                        this._logger.LogInformation("Streaming session ended with {EndPoint}. {Reason}", endPoint, breakReason);
                    }
                }
                else
                {
                    this._logger.LogError("Received an end of streaming packet from {EndPoint} while the streaming session is active with {StreamingEndPoint}, disconnecting...", endPoint, this._avaloniaManager.StreamingClientEndPoint);
                    this._sessionManager.Disconnect(endPoint.ToString());
                }
            }
        }

        private async Task ProcessDesktopFramePacket(DesktopFramePacket packet, IPEndPoint endPoint)
        {
            if (packet.ImageData == null)
            {
                this._logger.Log(LogLevel.None, "Failed to save an empty screenshot from {EndPoint}", endPoint);
                return;
            }

            string savePath = Path.GetFullPath(PathResolver.GetResolvedPath(
                this._streamingConfig.ScreenshotsFilePath,
                fileName: "%endPoint%_%datetime%",
                fileFormat: Enum.GetName(typeof(ScreenFormats), packet.FormatID)?.ToLower() ?? string.Empty
            ));

            try
            {
                string? directory = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(savePath, packet.ImageData.AsMemory(0, packet.ImageLength));
                this._logger.Log(LogLevel.None, "Screenshot from {EndPoint} successfully saved on path: {FilePath}", endPoint, savePath);
            }
            catch (Exception ex)
            {
                this._logger.LogError("Failed to save screenshot: {Exception}", ex);
            }
        }

        private void ProcessStreamFramePacket(StreamFramePacket packet, IPEndPoint endPoint)
        {
            if (this._sessionManager.GetClientSession(endPoint.ToString(), out IServerClientSession? session))
            {
                IPEndPoint? actualStreamer = this._avaloniaManager.StreamingClientEndPoint;
                if (actualStreamer == null)
                {
                    this._logger.LogWarning("Received a streaming frame from {EndPoint} while no streaming session is active, nothing to do", endPoint);
                    return;
                }

                if (session!.RemoteEndPoint != actualStreamer)
                {
                    this._logger.LogError("Received a streaming frame from {EndPoint} while the streaming session is active with {StreamingEndPoint}, disconnecting...", endPoint, _avaloniaManager.StreamingClientEndPoint);
                    this._sessionManager.Disconnect(endPoint.ToString());
                    return;
                }

                if (packet.Patches == null || packet.PatchesCount == 0)
                {
                    this._logger.LogError("Received an empty streaming frame from {EndPoint}, disconnecting...", endPoint);
                    this._sessionManager.Disconnect(endPoint.ToString());
                    return;
                }
                this._avaloniaManager.UpdateBitmap(packet.Patches, packet.PatchesCount, packet.IsFullFrame);
            }
        }

        private void ProcessPartingPacket(PartingPacket packet, IPEndPoint endPoint)
        {
            this._logger.LogInformation("Received a parting packet from {EndPoint} with status code {StatusCode} ({StatusName})", endPoint, packet.StatusCode, Enum.GetName(typeof(PartingStatusCodes), packet.StatusCode));
            this._logger.Log(LogLevel.None, "Total {EndPoint} uptime: {Uptime} | received: {ReceivedPackets} | sent: {SentPackets}", endPoint, TimeSpan.FromSeconds(packet.UptimeSecs).ToString(@"dd\.hh\:mm\:ss"), packet.ReceivedPackets, packet.SentPackets);
            this._sessionManager.Disconnect(endPoint.ToString());
        }
    }
}
