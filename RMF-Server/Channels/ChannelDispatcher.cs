using DynamicData;
using RMF.Core.Interfaces;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF_Server.Debugger;
using RMF_Server.Logic;
using RMF_Server.Packets;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace RMF_Server.Channels
{
    internal static class ChannelDispatcher
    {
        private static readonly Dictionary<int, ChannelContext> Channels = [];

        private static async Task InboundChannelWorker(Channel<PacketContext> channel, int id = 0, CancellationToken? token = null)
        {
            ChannelReader<PacketContext> reader = channel.Reader;

            try
            {
                await foreach (PacketContext context in reader.ReadAllAsync(token ?? CancellationToken.None))
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
                Logging.Output($"Channel for key {id} has been closed");
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
                if (Channels.ContainsKey(k))
                {
                    Logging.Warning($"Failed to open channel for key {k}, it already exists");
                    continue;
                }

                Channel<PacketContext> rawChannel = Channel.CreateBounded<PacketContext>(new BoundedChannelOptions(ConfigurationManager.ChannelPacketsCapacity)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true
                });
                CancellationTokenSource cts = new();
                Task workerTask = InboundChannelWorker(rawChannel, id: k, token: cts.Token);

                Channels[k] = new ChannelContext(
                    rawChannel,
                    workerTask,
                    cts
                );
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
            await Channels[channelKey].Channel.Writer.WriteAsync(context);
        }

        public static async Task CloseChannels()
        {
            int terminateChannelsCounter = 0;
            int totalActiveChannels = Channels.Count;
            
            List<Task> terminationTasks = [];
            foreach (ChannelContext context in Channels.Values)
            {
                if (!context.Worker.IsCompleted)
                {
                    context.TokenSource.Cancel();
                    terminationTasks.Add(context.Worker);
                    terminateChannelsCounter++;
                }
            }
            await Task.WhenAll(terminationTasks);

            Logging.Output($"Successfully closed {terminateChannelsCounter} channels out of {totalActiveChannels} active");
            Channels.Clear();
        }

        public static bool IsChannelExists(int key)
        {
            return Channels.ContainsKey(key);
        }
    }
}
