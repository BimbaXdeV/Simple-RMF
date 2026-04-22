using RMF.Core.Bases;
using RMF.Core.Interfaces;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF.Core.Packets.Client;
using RMF.Core.Screen;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events.Client
{
    public class StreamingEvent : BackgroundEvent
    {
        public IScreenProvider? Provider { get; set; }
        public ScreenFormats Format { get; set; }
        public byte QualityPercent { get; set; }
        public int FrameUpdateRate { get; set; }
        public int IntervalMsecs { get; set; }  // (default) 0 - without delay

        private void SendActualFrame(ClientSession session)
        {
            CapturedFrame? frame = this.Provider?.Capture(this.Format, this.QualityPercent, this.FrameUpdateRate);
            if (frame != null && frame.Value is CapturedFrame f)
            {
                StreamFramePacket streamFramePacket = new()
                {
                    FormatID = (byte)f.Format,
                    Patches = f.Rects,
                    PatchesCount = f.RectsCount,
                    IsFullFrame = f.IsFullFrame,
                };

                session.SendPacket(streamFramePacket);
            }
        }

        protected override async Task HandleLogic(ClientSession session, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                SendActualFrame(session);
                await Task.Delay(this.IntervalMsecs, token);
            }
        }
    }
}
