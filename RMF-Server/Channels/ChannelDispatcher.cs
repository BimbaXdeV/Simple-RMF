using RMF.Core.Packets;
using RMF_Server.Packets;
using RMF_Server.Storage;
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
        private static readonly Dictionary<int, Channel<PacketContext>> Channels = new();
        //private static readonly ChannelWriter<byte[]> Writer = new();

        public static void OpenFound()
        {
            HashSet<int> channelKeys = PacketsAssembler.GetRegisteredIDs().Select(x => x / 100).ToHashSet();

            foreach (int k in channelKeys)
            {
                Channels[k] = Channel.CreateUnbounded<PacketContext>();
            }
        }

        public static void CloseAll()
        {
            foreach (Channel<PacketContext> c in Channels.Values)
            {
                c.Writer.Complete();
            }
            Channels.Clear();
        }

        public Channel<PacketContext>? GetChannel(int key)
        {
            return Channels.GetValueOrDefault(key);
        }
    }
}
