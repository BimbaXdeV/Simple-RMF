using RMF.Core.Interfaces;
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

        private static async Task OpenChannel(Channel<PacketContext> channel, int id = 0)
        {
            ChannelReader<PacketContext> reader = channel.Reader;

            try
            {
                await foreach (PacketContext context in reader.ReadAllAsync())
                {
                    Packet? packet = PacketsAssembler.GetPacket(context.ID);
                    if (packet == null)
                    {
                        Logging.Warning($"Received an unknown packet \"{context.ID}\" from the client {context.EndPoint}");
                        ArrayPool<byte>.Shared.Return(context.Payload);
                        continue;
                    }

                    try
                    {
                        ReadOnlySpan<byte> payloadSpan = context.Payload.AsSpan(0, context.Length);
                        SpanReader payloadReader = new(payloadSpan);

                        packet.Deserialize(ref payloadReader);
                        await PacketsProcessor.SwitchHandle(packet, context.EndPoint);  // When scaling, a new case needs to be added
                    }
                    catch (Exception ex)
                    {
                        Logging.Warning($"Failed to process packet with ID {context.ID} from {context.EndPoint}\n{ex}");
                    }
                    finally
                    {
                        // To avoid allocating unnecessary memory, we allocate a free byte[] from the async pool, which must be returned after use
                        ArrayPool<byte>.Shared.Return(context.Payload);
                        if (packet is IReleasable releasable)
                        {
                            releasable.Release();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                channel.Writer.Complete();
                Logging.Message($"Channel for key {id} has been closed");
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
                Channel<PacketContext> rawChannel = Channel.CreateBounded<PacketContext>(new BoundedChannelOptions(ConfigurationManager.ChannelPacketsCapacity)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true
                });
                if (!Channels.TryAdd(k, rawChannel))
                {
                    Logging.Warning($"Failed to initialize channel for key {k}");
                    continue;
                }
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
                ArrayPool<byte>.Shared.Return(context.Payload);
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
