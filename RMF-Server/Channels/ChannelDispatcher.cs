using RMF.Core.Network;
using RMF.Core.Packets;
using RMF_Server.Debugger;
using RMF_Server.Logic;
using RMF_Server.Packets;
using RMF_Server.Storage;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RMF_Server.Channels
{
    internal static class ChannelDispatcher
    {
        private static readonly Dictionary<int, Channel<PacketContext>> Channels = new();
        //private static readonly ChannelWriter<byte[]> Writer = new();

        private static async Task OpenChannel(Channel<PacketContext> channel)
        {
            ChannelReader<PacketContext> reader = channel.Reader;

            await foreach (PacketContext context in reader.ReadAllAsync())
            {
                Packet? packet = PacketsAssembler.GetPacket(context.ID);
                if (packet == null)
                {
                    Logging.Warning($"Received an unknown packet \"{context.ID}\" from the client {context.EndPoint}");
                    continue;
                }

                ReadOnlySpan<byte> payloadSpan = context.Payload.AsSpan(0, context.Length);
                SpanReader payloadReader = new(payloadSpan);

                packet.Deserialize(ref payloadReader);
                PacketsHandler.SwitchHandle(packet, context.EndPoint);  // When scaling, a new case needs to be added
            }
        }

        public static (int, int) StartFound()
        {
            HashSet<int> channelKeys = PacketsAssembler.GetClientPacketsIDs().Select(x => x / 100).ToHashSet();
            if (channelKeys.Count == 0)
            {
                Logging.Warning("Failed to get IDs of existing packages. Make sure you have already loaded all packages into RMF.Core.Packets.PacketAssembler before calling");
                return (0, 0);
            }

            int initializedChannelsCounter = 0;
            foreach (int k in channelKeys)
            {
                Console.WriteLine(k);
                Channel<PacketContext> rawChannel = Channel.CreateBounded<PacketContext>(new BoundedChannelOptions(ConfigurationManager.ChannelPacketsCapacity)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true
                });
                
                Channels[k] = Channel.CreateUnbounded<PacketContext>();
                _ = Task.Run(() => OpenChannel(rawChannel));
                initializedChannelsCounter++;
            }
            return (initializedChannelsCounter, channelKeys.Count);
        }

        public static async Task SendPacket(PacketContext context)
        {
            int channelKey = context.ID / 100;
            if (!IsChannelExists(channelKey))
            {
                // Just in case OpenTCP validator suffers changes in structure
                Logging.Warning($"Unable to find an open channel for packet {context.ID} reveiced from {context.EndPoint}");
                return;
            }
            await Channels[channelKey].Writer.WriteAsync(context);
        }

        public static void CloseAll()
        {
            foreach (Channel<PacketContext> c in Channels.Values)
            {
                c.Writer.Complete();
            }
            Channels.Clear();
        }

        public static bool IsChannelExists(int key)
        {
            return Channels.ContainsKey(key);
        }

        public static Channel<PacketContext>? GetChannel(int key)
        {
            return Channels.GetValueOrDefault(key);
        }
    }
}
