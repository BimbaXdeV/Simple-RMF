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

        public static async Task OpenChannel(Channel<PacketContext> channel)
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

                MemoryStream ms = NetworkBuffer.GetMemoryStream(resetMemory: true);
                ms.Write(context.Payload, 0, context.Length);
                ms.Position = 0;

                BinaryReader payloadReader = NetworkBuffer.GetBinaryReader();  // It will also call method GetMemoryStram(), but it will not accidentally clear all memory

                packet.Deserialize(payloadReader);
                PacketsHandler.SwitchHandle(packet, context.EndPoint);  // When scaling, a new case needs to be added
            }
        }

        public static void StartFound()
        {
            HashSet<int> channelKeys = PacketsAssembler.GetClientPacketsIDs().Select(x => x / 100).ToHashSet();
            if (channelKeys.Count == 0)
            {
                Logging.Warning("Failed to get IDs of existing packages. Make sure you have already loaded all packages into RMF.Core.Packets.PacketAssembler before calling");
                return;
            }

            Channel<PacketContext>? rawChannel;
            foreach (int k in channelKeys)
            {
                rawChannel = Channel.CreateBounded<PacketContext>(new BoundedChannelOptions(ConfigurationManager.ChannelPacketsCapacity)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true
                });
                Channels[k] = Channel.CreateUnbounded<PacketContext>();
                _ = Task.Run(() => OpenChannel(rawChannel));
            }
        }

        public static async Task SendPacket(PacketContext context)
        {
            int channelKey = context.ID / 100;
            if (!IsChannelExists(channelKey))
            {
                // Just in case OpenTCP validator suffers changes in structure
                Logging.Warning($"Failed to send packet with ID {context.ID} from {context.EndPoint}, the corresponding channel does not exist");
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
