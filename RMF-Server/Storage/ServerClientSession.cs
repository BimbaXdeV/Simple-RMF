using RMF.Core.Bases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RMF_Server.Storage
{
    internal class ServerClientSession : ClientSession
    {
        private int _rateLimitCounter;
        private long _lastResetTicks;

        public ServerClientSession(
            TcpClient client,
            int channelCapacity = 0,
            bool collectingStats = false,
            CancellationToken token = default
        ) : base(client, channelCapacity, collectingStats, token)
        {
            this._lastResetTicks = DateTime.UtcNow.Ticks;
        }

        public bool IsRateLimitExceed(int maxRate)
        {
            long currentTicks = DateTime.UtcNow.Ticks;
            long lastReset = Interlocked.Read(ref this._lastResetTicks);

            if (currentTicks - lastReset >= TimeSpan.TicksPerSecond)
            {
                if (Interlocked.CompareExchange(ref _lastResetTicks, currentTicks, lastReset) == lastReset)
                {
                    Interlocked.Exchange(ref this._rateLimitCounter, 0);
                }
            }

            int currentRate = Interlocked.Increment(ref this._rateLimitCounter);
            return currentRate > maxRate;
        }
    }
}
