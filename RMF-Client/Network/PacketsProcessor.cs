using RMF.Core.Packets.Client;
using RMF.Core.Packets;
using RMF.Core.Packets.Server;
using RMF_Client.Logic;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Reflection;
using RMF_Client.Storage;
using System.Net;
using RMF_Client.Capture;
using RMF.Core.Events;
using RMF.Core.Screen;
using RMF.Core.Interfaces;
using RMF_Client.Monitors;

namespace RMF_Client.Network
{
    internal static class PacketsProcessor
    {
        public static void SwitchHandle(Packet packet)
        {
            switch (packet)
            {
                case HandshakePacket handshakePacket:
                    ProcessHandshakePacket(handshakePacket);
                    break;

                case ClientVersionRequest clientVersionRequest:
                    ProcessClientVersionRequest(clientVersionRequest);
                    break;

                case ClientInfoRequest clientInfoRequest:
                    ProcessClientInfoRequest(clientInfoRequest);
                    break;

                case ClientPingRequest clientPingRequest:
                    ProcessClientPingRequest(clientPingRequest);
                    break;

                case ScreenshotRequest screenshotRequest:
                    ProcessScreenshotRequest(screenshotRequest);
                    break;

                case StreamingRequest streamingRequest:
                    ProcessStreamingRequest(streamingRequest);
                    break;

                case EndOfEventsRequest endOfEventsRequest:
                    ProcessEndOfEventsRequest(endOfEventsRequest);
                    break;
            }
        }

        public static void SearchHandle(Packet packet)
        {
            var method = typeof(PacketsProcessor).GetMethod("Process" + packet.GetType().Name, BindingFlags.NonPublic | BindingFlags.Static);
            method?.Invoke(null, new object[] { packet });
        }

        private static void ProcessHandshakePacket(HandshakePacket packet)
        {
            IPEndPoint? remoteEndpoint = SessionManager.Connection?.Client.Client.RemoteEndPoint as IPEndPoint;
            int localPort = remoteEndpoint?.Port ?? -1;

            AppearanceManager.ReplaceToolbarContent(new Dictionary<string, string>
            {
                { "endpointTime", DateTimeOffset.FromUnixTimeMilliseconds(packet.ConnectionTimestamp).LocalDateTime.ToString("HH:mm:ss") },
                { "endpointID", packet.SessionID.ToString() },
                { "endpointIP", packet.RemoteIP ?? "0.0.0.0" },
                { "endpointPort", $"{localPort} ({packet.RemotePort})" },
                { "endpointBuffer", $"Sd: {packet.SendBufferSize}b, Rc: {packet.ReceiveBufferSize}b" }
            });
        }

        private static void ProcessClientVersionRequest(ClientVersionRequest packet)
        {
            ConnectionClientSession session = SessionManager.Connection!;
            Version? appVersion = Assembly.GetEntryAssembly()?.GetName().Version;
            Version? coreVersion = typeof(Packet).Assembly.GetName().Version;

            ClientVersionPacket versionPacket = new()
            {
                AppMajorVersion = (short)(appVersion?.Major ?? 0),
                AppMinorVersion = (short)(appVersion?.Minor ?? 0),
                AppBuildVersion = (short)(appVersion?.Build ?? 0),
                CoreMajorVersion = (short)(coreVersion?.Major ?? 0),
                CoreMinorVersion = (short)(coreVersion?.Minor ?? 0),
                CoreBuildVersion = (short)(coreVersion?.Build ?? 0)
            };
            session.SendPacket(versionPacket);
        }

        private static void ProcessClientInfoRequest(ClientInfoRequest packet)
        {
            ConnectionClientSession session = SessionManager.Connection!;
            IHardwareMonitor? hardwareMonitor = MonitoringFactory.GetActualMonitor(updateIfNullable: true);
            if (hardwareMonitor != null)
            {
                ClientInfoPacket clientInfoPacket = new()
                {
                    MachineName = hardwareMonitor.MachineName(),
                    Username = hardwareMonitor.Username(),
                    OSName = hardwareMonitor.OSName(),
                    CPUName = hardwareMonitor.CPUName(),
                    CPUArchitecture = hardwareMonitor.CPUArchitecture(),
                    GPUName = hardwareMonitor.GPUName(),
                    RAMCapacity = (long)hardwareMonitor.RAMCapacity(),
                    VRAMCapacity = (long)hardwareMonitor.VRAMCapacity()
                };
                session.SendPacket(clientInfoPacket);
            }
        }

        private static void ProcessClientPingRequest(ClientPingRequest packet)
        {
            ConnectionClientSession session = SessionManager.Connection!;

            HeartbeatPacket heartbeatPacket = new()
            {
                TurnedTimestamp = packet.SendingTimestamp
            };
            session.SendPacket(heartbeatPacket);
        }

        private static void ProcessScreenshotRequest(ScreenshotRequest packet)
        {
            ConnectionClientSession session = SessionManager.Connection!;
            IScreenProvider? screenProvider = CaptureFactory.GetActualProvider(updateIfNullable: true);
            if (screenProvider == null)
            {
                return;
            }

            CapturedFrame? screenshot = screenProvider.Capture((ScreenFormats)packet.FormatID, packet.QualityPercent, 0);
            if (screenshot.HasValue)
            {
                CapturedFrame frame = screenshot.Value;
                DesktopFramePacket desktopFramePacket = new()
                {
                    FormatID = packet.FormatID,
                    Width = frame.Rects[0].Width,
                    Height = frame.Rects[0].Height,
                    ImageLength = frame.Rects[0].Length,
                    ImageData = frame.Rects[0].Data
                };

                session.SendPacket(desktopFramePacket);
            }
        }

        private static void ProcessStreamingRequest(StreamingRequest packet)
        {
            ConnectionClientSession session = SessionManager.Connection!;

            // Streaming shutdown logic : if the packet includes "IsActive = false" and streaming event has already running
            bool isEventActive = session.Events.IsRunning("StreamingEvent");
            if (!packet.IsActive && isEventActive)
            {
                session.Events.StopEvent("StreamingEvent");
                return;
            }

            // Launch streaming loop
            if (packet.IsActive && !isEventActive)
            {
                IScreenProvider? screenProvider = CaptureFactory.GetActualProvider(updateIfNullable: true);
                if (screenProvider == null)
                {
                    return;
                }

                session.Events.StartEvent(session, "StreamingEvent", new Dictionary<string, object>
                {
                    { "Provider", screenProvider },
                    { "Format", (ScreenFormats)packet.FormatID },
                    { "QualityPercent", packet.Quality },
                    { "FrameUpdateRate", packet.FrameUpdateRate },
                    { "TargetFPS", packet.TargetFPS }
                });
            }
        }

        private static void ProcessEndOfEventsRequest(EndOfEventsRequest packet)
        {
            ConnectionClientSession session = SessionManager.Connection!;
            session.Events.StopAllRunning();

            long clientUptime = session.ConnectedTime != default ? (long)(DateTime.UtcNow - session.ConnectedTime).TotalSeconds : -1;
            PartingPacket partingPacket = new()
            {
                StatusCode = 0,
                UptimeSecs = clientUptime,
                ReceivedPackets = session.TotalPacketsReceived,
                SentPackets = session.TotalPacketsSent,
                LastTransferedTimestamp = new DateTimeOffset(session.LastTransferTime).ToUnixTimeMilliseconds()
            };
            session.SendPacket(partingPacket);
        }
    }
}
