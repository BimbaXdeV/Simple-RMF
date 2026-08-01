using RMF.Core.Bases;
using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal class ServerClientSession : ClientSession, IServerClientSession
    {
        private int _rateLimitCounter;
        private long _lastResetTicks;

        public ServerClientSession(
            INetworkConnection connection,
            IProtocolReader reader,
            IPacketSender packetSender,
            int channelCapacity = 0,
            bool collectingStats = false,
            CancellationToken token = default
        ) : base(connection, reader, packetSender, channelCapacity, collectingStats, token)
        {
            _lastResetTicks = DateTime.UtcNow.Ticks;
        }

        public bool IsRateLimitExceed(int maxRate)
        {
            long currentTicks = DateTime.UtcNow.Ticks;
            long lastReset = Interlocked.Read(ref _lastResetTicks);

            if (currentTicks - lastReset >= TimeSpan.TicksPerSecond)
            {
                if (Interlocked.CompareExchange(ref _lastResetTicks, currentTicks, lastReset) == lastReset)
                {
                    Interlocked.Exchange(ref _rateLimitCounter, 0);
                }
            }

            int currentRate = Interlocked.Increment(ref _rateLimitCounter);
            return currentRate > maxRate;
        }
    }
}
