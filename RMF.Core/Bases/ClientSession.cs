using RMF.Core.Events;
using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Network;
using RMF.Core.Network;
using RMF.Core.Packets;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RMF.Core.Bases
{
    public abstract class ClientSession : ISession
    {
        protected readonly INetworkConnection Connection;
        protected readonly IProtocolReader Reader;
        protected readonly IPacketSender PacketSender;

        private readonly EventController _events;
        protected Channel<Packet> OutboundChannel { get; private set; }
        public bool IsRunning { get; private set; }

        public IPEndPoint RemoteEndPoint => this.Connection.RemoteEndPoint;
        public int SendBufferSize => this.Connection.SendBufferSize;
        public int ReceiveBufferSize => this.Connection.ReceiveBufferSize;

        public bool CollectingStats { get; private set; }
        private long _totalPacketsSent;
        public long TotalPacketsSent => Interlocked.Read(ref this._totalPacketsSent);

        private long _totalPacketsReceived;
        public long TotalPacketsReceived => Interlocked.Read(ref this._totalPacketsReceived);
        
        private long _lastTransferTimeTicks;
        public long LastTransferTimeTicks => Interlocked.Read(ref this._lastTransferTimeTicks);
        public DateTime LastTransferTime => new(Interlocked.Read(ref this._lastTransferTimeTicks), DateTimeKind.Utc);

        private readonly SemaphoreSlim _streamLocker = new(1, 1);

        public ClientSession(
            INetworkConnection connection,
            IProtocolReader reader,
            IPacketSender packetSender,
            IEventFactory eventFactory,
            int channelCapacity = 0,
            bool collectingStats = false,
            CancellationToken token = default
        )
        {
            this.Connection = connection;
            this.Reader = reader;
            this.PacketSender = packetSender;

            this._events = new EventController(eventFactory);
            this.OutboundChannel = Channel.CreateBounded<Packet>(
                new BoundedChannelOptions(channelCapacity > 0 ? channelCapacity : 1000)
                {
                    FullMode = BoundedChannelFullMode.Wait
                }
            );
            this.CollectingStats = collectingStats;
            RunProcessing(token);
        }

        public void RunProcessing(CancellationToken token)
        {
            if (this.IsRunning)
            {
                return;
            }
            this.IsRunning = true;
            _ = Task.Run(() => OutboundChannelWorker(token));  // Each session has its own packet sender
        }

        private async Task OutboundChannelWorker(CancellationToken token)
        {
            if (!this.IsRunning)
            {
                return;
            }

            try
            {
                await foreach (Packet packet in this.OutboundChannel.Reader.ReadAllAsync(token))
                {
                    await _streamLocker.WaitAsync(token);
                    try
                    {
                        await this.PacketSender.SendPacketAsync(this.Connection.GetNetworkStream(), packet, token);
                        IncrementSendPackets();
                    }
                    finally
                    {
                        if (packet is IReleasable releasable)
                        {
                            releasable.Release();
                        }
                        _streamLocker.Release();
                    }
                }
            }
            finally
            {
                while (this.OutboundChannel.Reader.TryRead(out Packet? packet))
                {
                    if (packet is IReleasable releasable)
                    {
                        releasable.Release();
                    }
                }
            }
        }

        public Task<PacketHeader> ReadHeaderAsync(CancellationToken token)
        {
            return this.Reader.ReadHeaderAsync(this.Connection.GetNetworkStream(), token);
        }

        public Task<byte[]> ReadPayloadAsync(int length, CancellationToken token)
        {
            return this.Reader.ReadPayloadAsync(this.Connection.GetNetworkStream(), length, token);
        }

        public void SendPacket(Packet packet)
        {
            if (!this.IsRunning)
            {
                if (packet is IReleasable releasable)
                {
                    releasable.Release();
                }
                return;
            }

            if (this.OutboundChannel.Writer.TryWrite(packet))
            {
                return;
            }

            this.OutboundChannel.Reader.TryRead(out Packet? oldestPacket);
            if (oldestPacket != null && oldestPacket is IReleasable releasableOldest)
            {
                releasableOldest.Release();
            }

            if (!this.OutboundChannel.Writer.TryWrite(packet) && packet is IReleasable releasableDuplication)
            {
                releasableDuplication.Release();
            }
        }

        public void StartEvent(string eventName, Dictionary<string, object> eventSettings)
        {
            this._events.StartEvent(this, eventName, eventSettings);
        }

        public void IncrementSendPackets()
        {
            if (this.CollectingStats)
            {
                Interlocked.Increment(ref this._totalPacketsSent);
                Interlocked.Exchange(ref this._lastTransferTimeTicks, DateTime.UtcNow.Ticks);
            }
        }

        public void IncrementReceivedPackets()
        {
            if (this.CollectingStats)
            {
                Interlocked.Increment(ref this._totalPacketsReceived);
                Interlocked.Exchange(ref this._lastTransferTimeTicks, DateTime.UtcNow.Ticks);
            }
        }

        public void StopProcessing()
        {
            this.IsRunning = false;
            this.OutboundChannel.Writer.TryComplete();
            this._events.StopAllRunning();
            this.Connection.Close();
        }
    }
}
