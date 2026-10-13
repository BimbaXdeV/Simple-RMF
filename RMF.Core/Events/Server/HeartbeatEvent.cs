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
        public int IntervalSecs { get; set; }

        protected override async Task HandleLogic(ISession session, CancellationToken token)
        {
            if (this.IntervalSecs <= 0)
            {
                return;
            }

            ClientPingRequest pingRequest = new ClientPingRequest();
            while (!token.IsCancellationRequested)
            {
                pingRequest.SendingTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                session.SendPacket(pingRequest);
                await Task.Delay(this.IntervalSecs * 1000, token);
            }
        }

        public override bool ValidateSettings(out string? exceptionMessage)
        {
            exceptionMessage = null;
            if (this.IntervalSecs <= 0)
            {
                exceptionMessage = "IntervalSecs must be greater than 0";
                return false;
            }
            return true;
        }

        public override void ApplySettings(IReadOnlyDictionary<string, object> settings)
        {
            ApplyValue<int>(settings, nameof(IntervalSecs), v => IntervalSecs = v);
        }
    }
}
