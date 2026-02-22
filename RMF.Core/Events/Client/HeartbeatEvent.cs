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
        public override string EvName => "HeartbeatEvent";
        public float IntervalSecs { get; set; } = 5f;

        protected override async Task BackgroundEvLogic(Stream stream, CancellationToken token)
        {
            HeartbeatPacket heartbeatPacket = new();
            while (!token.IsCancellationRequested)
            {
                heartbeatPacket.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await StreamManager.SendPacketAsync(stream, heartbeatPacket, token);
                await Task.Delay((int)(this.IntervalSecs * 1000), token);
            }
        }
    }
}
