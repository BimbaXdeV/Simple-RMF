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

        private static void ProcessClientPingRequest(ClientPingRequest packet)
        {
        }

        private static void ProcessStreamingRequest(StreamingRequest packet)
        {
        }
    }
}
