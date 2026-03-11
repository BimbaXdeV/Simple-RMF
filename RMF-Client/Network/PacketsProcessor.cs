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

                case ClientPingRequest clientPingRequest:
                    ProcessClientPingRequest(clientPingRequest);
                    break;

                case ScreenshotRequest screenshotRequest:
                    ProcessScreenshotRequest(screenshotRequest);
                    break;

                case StreamingRequest streamingRequest:
                    ProcessStreamingRequest(streamingRequest);
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
                { "endpointTime", DateTimeOffset.FromUnixTimeMilliseconds(packet.ConnectionTimestamp).ToString("HH:mm:ss") },
                { "endpointID", packet.SessionID.ToString() },
                { "endpointIP", packet.RemoteIP ?? "0.0.0.0" },
                { "endpointPort", $"{localPort} ({packet.RemotePort})" },
                { "endpointBuffer", $"Sd: {packet.SendBufferSize}b, Rc: {packet.ReceiveBufferSize}b" }
            });
        }

        private static void ProcessClientPingRequest(ClientPingRequest packet)
        {
            NetworkStream? stream = SessionManager.Connection?.Client.GetStream();
            if (stream != null)
            {
                SessionManager.Connection!.Events.ToggleEvent(SessionManager.Connection, "HeartbeatEvent", new Dictionary<string, object>
                {
                    { "IntervalSecs", packet.IntervalSecs }
                });
            }
        }

        private static void ProcessScreenshotRequest(ScreenshotRequest packet)
        {
            NetworkStream? stream = SessionManager.Connection?.Client.GetStream();
            IScreenProvider? screenProvider = CaptureFactory.GetActualProvider(UpdateIfNullable: true);
            if (stream != null && screenProvider != null)
            {
                //SessionManager.Connection!.Events.ToggleEvent(SessionManager.Connection, "StreamingEvent", new Dictionary<string, object>
                //{
                //    { "Provider", screenProvider },
                //    { "ProcessMode", ProcessModes.Single },
                //    { "Format", (ScreenFormats)packet.FormatID },
                //    { "QualityPercent", packet.QualityPercent }
                //});

                CapturedFrame? screenshot = screenProvider.Capture((ScreenFormats)packet.FormatID, packet.QualityPercent);
                if (screenshot != null)
                {
                    DesktopFramePacket desktopFramePacket = new()
                    {
                        FormatID = packet.FormatID,
                        Width = screenshot.Value.Width,
                        Height = screenshot.Value.Height,
                        ImageLength = screenshot.Value.Length,
                        ImageData = screenshot.Value.Buffer
                    };

                    SessionManager.Connection!.SendPacket(desktopFramePacket);
                }
            }
        }

        private static void ProcessStreamingRequest(StreamingRequest packet)
        {
        }
    }
}
