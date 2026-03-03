using RMF.Core.Bases;
using RMF.Core.Network;
using RMF.Core.Packets.Client;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events.Client
{
    public class HeartbeatEvent : BackgroundEvent
    {
        public int IntervalSecs { get; set; } = 1;

        protected override async Task HandleLogic(ClientSession session, CancellationToken token)
        {
            HeartbeatPacket heartbeatPacket = new();
            while (!token.IsCancellationRequested)
            {
                heartbeatPacket.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                session.SendPacket(heartbeatPacket);
                await Task.Delay(this.IntervalSecs * 1000, token);
            }
        }
    }
}
