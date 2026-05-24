using RMF.Core.Bases;
using RMF.Core.Network;
using RMF.Core.Packets.Client;
using RMF.Core.Packets.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events.Server
{
    public class HeartbeatEvent : BackgroundEvent
    {
        public int IntervalSecs { get; set; } = 1;

        protected override async Task HandleLogic(ClientSession session, CancellationToken token)
        {
            ClientPingRequest pingRequest = new ClientPingRequest();
            while (!token.IsCancellationRequested)
            {
                pingRequest.SendingTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                session.SendPacket(pingRequest);
                await Task.Delay(this.IntervalSecs * 1000, token);
            }
        }
    }
}
