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

namespace RMF_Client.Network
{
    internal static class PacketsProcessor
    {
        public static void SwitchHandle(Packet packet)
        {
            switch (packet)
            {
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

        private static void ProcessClientPingRequest(ClientPingRequest packet)
        {
            NetworkStream? stream = ConnectionSession.Client?.GetStream();
            if (stream != null)
            {
                ConnectionSession.Events.ToggleEvent(stream, "Heartbeat", new Dictionary<string, object>
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
