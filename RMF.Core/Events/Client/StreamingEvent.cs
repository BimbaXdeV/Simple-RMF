using RMF.Core.Bases;
using RMF.Core.Interfaces;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF.Core.Packets.Client;
using RMF.Core.Screen;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
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
        public int TargetFPS { get; set; }

        private void SendActualFrame(ClientSession session)
        {
            CapturedFrame? frame = this.Provider?.Capture(this.Format, this.QualityPercent, this.FrameUpdateRate);
            if (frame.HasValue && frame.Value is CapturedFrame f)
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
            double targetFrameRateTick = 1000.0 / this.TargetFPS;
            Stopwatch sw = new();

            while (!token.IsCancellationRequested)
            {
                sw.Restart();

                SendActualFrame(session);

                double elapsedMsecs = sw.Elapsed.TotalMilliseconds;
                double remainingMsecs = targetFrameRateTick - elapsedMsecs;

                if (remainingMsecs > 0)
                {
                    await Task.Delay((int)Math.Round(remainingMsecs), token);
                }
            }
        }
    }
}
