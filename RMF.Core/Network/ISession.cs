using RMF.Core.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Network
{
    public interface ISession
    {
        bool IsRunning { get; }
        IPEndPoint RemoteEndPoint { get; }
        int SendBufferSize { get; }
        int ReceiveBufferSize { get; }
        long TotalPacketsSent { get; }
        long TotalPacketsReceived { get; }
        DateTime LastTransferTime { get; }

        Task<PacketHeader> ReadHeaderAsync(CancellationToken token);
        Task<byte[]> ReadPayloadAsync(int length, CancellationToken token);

        void SendPacket(Packet packet);
        void StartEvent(string eventName, Dictionary<string, object> eventSettings);
        void IncrementSendPackets();
        void IncrementReceivedPackets();
        void StopProcessing();
    }
}
