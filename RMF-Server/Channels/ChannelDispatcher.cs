using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Logic;
using RMF.Core.Interfaces.Network;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF_Server.Configurations;
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
    internal class ChannelDispatcher : IChannelDispatcher
    {
        private readonly IServerPacketProcessor _packetProcessor;
        private readonly ILoggingEngine? _logger;
        private readonly ChannelConfig? _channelConfig;

        private readonly Dictionary<int, ChannelContext> _channels;

        public ChannelDispatcher(
            IServerPacketProcessor packetProcessor,
            ILoggingEngine? logger = null,
            ChannelConfig? channelConfig = null
        )
        {
            this._packetProcessor = packetProcessor;
            this._logger = logger;
            this._channelConfig = channelConfig;

            this._channels = [];
        }

        private async Task InboundChannelWorker(Channel<PacketContext> channel, int id = 0, CancellationToken? token = null)
        {
            ChannelReader<PacketContext> reader = channel.Reader;

            try
            {
                await foreach (PacketContext context in reader.ReadAllAsync(token ?? CancellationToken.None))
                {
                    Packet? packet = PacketsAssembler.GetPacket(context.Id);
                    if (packet == null)
                    {
                        this._logger?.Warning($"Received an unknown packet \"{context.Id}\" from the client {context.EndPoint}");
                        ArrayPool<byte>.Shared.Return(context.Payload);
                        continue;
                    }

                    try
                    {
                        ReadOnlySpan<byte> payloadSpan = context.Payload.AsSpan(0, context.Length);
                        SpanReader payloadReader = new(payloadSpan);

                        packet.Deserialize(ref payloadReader);
                        await this._packetProcessor.SwitchHandle(packet, context.EndPoint);  // When scaling, a new case needs to be added
                    }
                    catch (Exception ex)
                    {
                        this._logger?.Warning($"Failed to process packet with ID {context.Id} from {context.EndPoint}{Environment.NewLine}{ex}");
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
                this._logger?.Output($"Channel for key {id} has been closed");
            }
        }

        public (int, int) StartFound()
        {
            HashSet<int> channelKeys = PacketsAssembler.GetClientPacketsIDs().Select(x => x / 100).ToHashSet();
            if (channelKeys.Count == 0)
            {
                this._logger?.Warning("Failed to get IDs of existing packages. Make sure you have already loaded all packages into RMF.Core.Packets.PacketAssembler before calling");
                return (0, 0);
            }

            int initializedChannelsCounter = 0;
            foreach (int k in channelKeys)
            {
                if (this._channels.ContainsKey(k))
                {
                    this._logger?.Warning($"Failed to open channel for key {k}, it already exists");
                    continue;
                }

                Channel<PacketContext> rawChannel = this._channelConfig != null
                    ? Channel.CreateBounded<PacketContext>(new BoundedChannelOptions(this._channelConfig?.ChannelPacketsCapacity ?? 1)
                    {
                        FullMode = BoundedChannelFullMode.DropOldest,
                        SingleReader = true
                    })
                    : Channel.CreateUnbounded<PacketContext>();

                CancellationTokenSource cts = new();
                Task workerTask = InboundChannelWorker(rawChannel, id: k, token: cts.Token);

                this._channels[k] = new ChannelContext(
                    rawChannel,
                    workerTask,
                    cts
                );
            }
            return (initializedChannelsCounter, channelKeys.Count);
        }

        public async Task EnqueuePacketAsync(PacketContext context)
        {
            int channelKey = context.Id / 100;
            if (!IsChannelExists(channelKey))
            {
                // Just in case OpenTCP validator suffers changes in structure
                this._logger?.Warning($"Unable to find an open channel for packet {context.Id} reveiced from {context.EndPoint}");
                ArrayPool<byte>.Shared.Return(context.Payload);
                return;
            }
            await this._channels[channelKey].Channel.Writer.WriteAsync(context);
        }

        public async Task CloseChannels()
        {
            int terminateChannelsCounter = 0;
            int totalActiveChannels = this._channels.Count;
            
            List<Task> terminationTasks = [];
            foreach (ChannelContext context in this._channels.Values)
            {
                if (!context.Worker.IsCompleted)
                {
                    context.TokenSource.Cancel();
                    terminationTasks.Add(context.Worker);
                    terminateChannelsCounter++;
                }
            }
            await Task.WhenAll(terminationTasks);

            this._logger?.Output($"Successfully closed {terminateChannelsCounter} channels out of {totalActiveChannels} active");
            this._channels.Clear();
        }

        public bool IsChannelExists(int key)
        {
            return this._channels.ContainsKey(key);
        }
    }
}
