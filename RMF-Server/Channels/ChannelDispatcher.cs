using Avalonia.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Network;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF_Server.Configurations;
using RMF_Server.Logic;
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
    internal class ChannelDispatcher : BackgroundService, IChannelDispatcher
    {
        private readonly IPacketFactory _packetFactory;
        private readonly IServerPacketProcessor _packetProcessor;
        private readonly ILogger<ChannelDispatcher> _logger;
        private readonly ChannelConfig _channelConfig;

        private readonly Dictionary<int, ChannelContext> _channels;

        public ChannelDispatcher(
            IPacketFactory packetFactory,
            IServerPacketProcessor packetProcessor,
            ILogger<ChannelDispatcher> logger,
            ChannelConfig channelConfig
        )
        {
            this._packetFactory = packetFactory;
            this._packetProcessor = packetProcessor;
            this._logger = logger;
            this._channelConfig = channelConfig;

            this._channels = [];
        }

        protected override Task ExecuteAsync(CancellationToken token)
        {
            HashSet<int> channelKeys = this._packetFactory.GetClientPacketsIDs().Select(x => x / 100).ToHashSet();
            if (channelKeys.Count == 0)
            {
                this._logger.LogError("Failed to get IDs of existing packages. Make sure you have already loaded all packages into RMF.Core.Packets.PacketFactory before calling");
                return Task.CompletedTask;
            }

            foreach (int k in channelKeys)
            {
                if (this._channels.ContainsKey(k))
                {
                    this._logger.LogWarning("Failed to open channel for key {ChannelId}, it already exists", k);
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
                Task workerTask = Task.Factory.StartNew(
                    () => InboundChannelWorker(rawChannel, id: k, token: cts.Token),
                    TaskCreationOptions.LongRunning
                );

                this._channels.TryAdd(k, new ChannelContext(rawChannel, workerTask, cts));
            }

            this._logger.LogInformation("Successfully started {Loaded} inbound channels", this._channels.Count);
            return Task.CompletedTask;
        }

        private async Task InboundChannelWorker(Channel<PacketContext> channel, int id = 0, CancellationToken token = default)
        {
            ChannelReader<PacketContext> reader = channel.Reader;

            try
            {
                await Task.Yield();
                await foreach (PacketContext context in reader.ReadAllAsync(token))
                {
                    Packet? packet = this._packetFactory.CreatePacket(context.Id);
                    if (packet == null)
                    {
                        this._logger.LogWarning("Received an unknown packet \"{PacketId}\" from the client {EndPoint}", context.Id, context.EndPoint);
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
                        this._logger.LogError("Failed to process packet with ID {PacketId} from {EndPoint}\n{Exception}", context.Id, context.EndPoint, ex);
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
                this._logger.LogInformation("Channel for key {ChannelId} has been closed", id);
            }
        }

        public async Task EnqueuePacketAsync(PacketContext context)
        {
            int channelKey = context.Id / 100;
            if (!IsChannelExists(channelKey))
            {
                // Just in case NetworkEngine validator suffers changes in structure
                this._logger.LogWarning("Unable to find an open channel for packet {PacketId} reveiced from {EndPoint}", context.Id, context.EndPoint);
                ArrayPool<byte>.Shared.Return(context.Payload);
                return;
            }
            await this._channels[channelKey].Channel.Writer.WriteAsync(context);
        }

        public bool IsChannelExists(int key)
        {
            return this._channels.ContainsKey(key);
        }

        public override async Task StopAsync(CancellationToken token)
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

            this._logger.LogInformation("Successfully closed {ClosedChannels} channels out of {TotalChannels} active", terminateChannelsCounter, totalActiveChannels);
            this._channels.Clear();

            await base.StopAsync(token);
        }
    }
}
