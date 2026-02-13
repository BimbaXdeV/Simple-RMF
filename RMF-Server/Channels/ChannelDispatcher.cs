using RMF_Server.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RMF_Server.Channels
{
    internal class ChannelDispatcher
    {
        private static readonly Dictionary<int, Channel<byte[]>> Channels = new();

        public static void OpenFound()
        {
            PacketsAssembler.

            for (int i = 0; i < 10; i++)
            {
                Channels[i] = Channel.CreateUnbounded<byte[]>();
            }
        }

        public ChannelWriter<byte>? GetChannel(int index)
        {
            return Channels.ElementAtOrDefault(index);
        }
    }
}
