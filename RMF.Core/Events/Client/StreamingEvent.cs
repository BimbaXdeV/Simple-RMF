using RMF.Core.Bases;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF.Core.Packets.Client;
using RMF.Core.Screen;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events.Client
{
    public class StreamingEvent : BackgroundEvent
    {
        public IScreenProvider? Provider;
        public ProcessModes ProcessMode;
        public ScreenFormats Format;
        public byte QualityPercent;
        public int IntervalMsecs;  // (default) 0 - without delay

        private readonly RemoteDesktopPacket PacketTemplate = new();

        private void SendActualFrame(ClientSession session)
        {
            CapturedFrame frame = this.Provider?.Capture(this.Format, this.QualityPercent) ?? default;
            if (frame.Buffer != null)
            {
                this.PacketTemplate.FormatID = (byte)frame.Format;
                this.PacketTemplate.Width = frame.Width;
                this.PacketTemplate.Height = frame.Height;
                this.PacketTemplate.ImageLength = frame.Length;
                this.PacketTemplate.ImageData = frame.Buffer;

                session.SendPacket(this.PacketTemplate);
            }
        }

        protected override async Task HandleLogic(ClientSession session, CancellationToken token)
        {
            switch (this.ProcessMode)
            {
                case ProcessModes.Single:
                    SendActualFrame(session);
                    break;

                case ProcessModes.InfinityLoop:
                    while (!token.IsCancellationRequested)
                    {
                        SendActualFrame(session);
                        if (this.IntervalMsecs > 0)
                        {
                            await Task.Delay(this.IntervalMsecs, token);
                        }
                    }
                    break;
            }
        }
    }
}
