using RMF.Core.Bases;
using RMF.Core.Packets;
using RMF.Core.Packets.Client;
using RMF.Core.Screen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events.Client
{
    public class StreamingEvent : BackgroundEvent
    {
        public ProcessModes ProcessMode;
        public ScreenFormats Format;
        public byte QualityPercent;
        public int IntervalMsecs;  // (default) 0 - without delay

        protected override Task HandleLogic(ClientSession session, CancellationToken token)
        {
        }
    }
}
