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
                { "endpointIP", remoteEndpoint?.Address.ToString() ?? "0.0.0.0" },
                { "endpointPort", $"{localPort} ({packet.RemotePort})" },
                { "endpointBuffer", $"Sd: {packet.SendBufferSize}b, Rc: {packet.ReceiveBufferSize}b" }
            });
        }

        private static void ProcessClientPingRequest(ClientPingRequest packet)
        {
            NetworkStream? stream = SessionManager.Connection?.Client.GetStream();
            if (stream != null)
            {
                SessionManager.Connection?.Events?.ToggleEvent(SessionManager.Connection, "Heartbeat", new Dictionary<string, object>
                {
                    { "IntervalSecs", packet.IntervalSecs }
                });
            }
        }

        private static void ProcessStreamingRequest(StreamingRequest packet)
        {
        }
    }
}
