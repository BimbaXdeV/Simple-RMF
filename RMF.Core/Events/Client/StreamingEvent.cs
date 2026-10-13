using RMF.Core.Appearance;
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

        private void SendActualFrame(ISession session)
        {
            CapturedFrame? frame = Provider!.Capture(Format, QualityPercent, FrameUpdateRate);
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

        protected override async Task HandleLogic(ISession session, CancellationToken token)
        {
            double targetFrameRateTick = 1000.0 / TargetFPS;
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

        public override bool ValidateSettings(out string? exceptionMessage)
        {
            exceptionMessage = null;
            if (Provider == null)
            {
                exceptionMessage = "Screen provider is not set";
                return false;
            }
            if (QualityPercent > 100)
            {
                exceptionMessage = "QualityPercent must be between 0 and 100";
                return false;
            }
            if (FrameUpdateRate < 0)
            {
                exceptionMessage = "FrameUpdateRate must or be greater than or equal to 0";
                return false;
            }
            if (TargetFPS <= 0)
            {
                exceptionMessage = "TargetFPS must be greater than 0";
                return false;
            }
            return true;
        }

        public override void ApplySettings(IReadOnlyDictionary<string, object> settings)
        {
            ApplyValue<IScreenProvider>(settings, nameof(Provider), v => Provider = v);
            ApplyValue<ScreenFormats>(settings, nameof(Format), v => Format = v);
            ApplyValue<byte>(settings, nameof(QualityPercent), v => QualityPercent = v);
            ApplyValue<int>(settings, nameof(FrameUpdateRate), v => FrameUpdateRate = v);
            ApplyValue<int>(settings, nameof(TargetFPS), v => TargetFPS = v);
        }
    }
}
