using RMF.Core.Events;
using RMF.Core.Interfaces;
using RMF.Core.Network;
using RMF.Core.Packets;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RMF.Core.Bases
{
    public abstract class ClientSession
    {
        public TcpClient Client { get; private set; }
        public IPEndPoint? EndPoint => this.Client.Client.RemoteEndPoint as IPEndPoint;
        protected NetworkStream? Stream => this.Client.Connected ? this.Client.GetStream() : null;

        public EventController Events { get; private set; } = new();
        protected Channel<Packet> OutboundChannel { get; private set; }
        public bool IsRunning { get; private set; }

        public bool CollectingStats { get; private set; }
        private long _totalPacketsSent;
        public long TotalPacketsSent => Interlocked.Read(ref this._totalPacketsSent);

        private long _totalPacketsReceived;
        public long TotalPacketsReceived => Interlocked.Read(ref this._totalPacketsReceived);
        
        private long _lastTransferTimeTicks;
        public long LastTransferTimeTicks => Interlocked.Read(ref this._lastTransferTimeTicks);
        public DateTime LastTransferTime => new(Interlocked.Read(ref this._lastTransferTimeTicks));

        public ClientSession(
            TcpClient client,
            int channelCapacity = 0,
            bool collectingStats = false,
            CancellationToken token = default
        )
        {
            this.Client = client;
            this.OutboundChannel = Channel.CreateBounded<Packet>(
                new BoundedChannelOptions(channelCapacity > 0 ? channelCapacity : 1000)
                {
                    FullMode = BoundedChannelFullMode.Wait
                }
            );
            this.CollectingStats = collectingStats;

            if (client.Connected)
            {
                RunProcessing(token);
            }
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
                    try
                    {
                        await StreamManager.SendPacketAsync(this.Client.GetStream(), packet, token);

                        if (this.CollectingStats)
                        {
                            IncrementSendPackets();
                        }
                    }
                    finally
                    {
                        if (packet is IReleasable releasable)
                        {
                            releasable.Release();
                        }
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

        public void RunProcessing(CancellationToken token)
        {
            if (this.IsRunning)
            {
                return;
            }
            this.IsRunning = true;
            _ = Task.Run(() => OutboundChannelWorker(token));  // Each session has its own packet sender
        }

        public void StopProcessing()
        {
            this.IsRunning = false;
            this.OutboundChannel.Writer.TryComplete();
            this.Events.StopAllRunning();
            this.Client.Close();
        }

        public void IncrementSendPackets()
        {
            Interlocked.Increment(ref this._totalPacketsSent);
            Interlocked.Exchange(ref this._lastTransferTimeTicks, DateTime.UtcNow.Ticks);
        }

        public void IncrementReceivedPackets()
        {
            Interlocked.Increment(ref this._totalPacketsReceived);
            Interlocked.Exchange(ref this._lastTransferTimeTicks, DateTime.UtcNow.Ticks);
        }
    }
}
