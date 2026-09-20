using RMF.Core.Events;
using RMF.Core.Interfaces;
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

namespace RMF.Core.Network
{
    public abstract class ClientSession : ISession
    {
        protected readonly INetworkConnection Connection;
        protected readonly IProtocolReader Reader;
        protected readonly IPacketSender PacketSender;
        protected readonly EventController Events;
        protected Channel<Packet> OutboundChannel { get; private set; }

        public bool IsRunning { get; private set; }
        public IPEndPoint RemoteEndPoint => Connection.RemoteEndPoint;
        public int SendBufferSize => Connection.SendBufferSize;
        public int ReceiveBufferSize => Connection.ReceiveBufferSize;

        public bool CollectingStats { get; private set; }
        private long _totalPacketsSent;
        public long TotalPacketsSent => Interlocked.Read(ref _totalPacketsSent);

        private long _totalPacketsReceived;
        public long TotalPacketsReceived => Interlocked.Read(ref _totalPacketsReceived);

        private long _lastTransferTimeTicks;
        public long LastTransferTimeTicks => Interlocked.Read(ref _lastTransferTimeTicks);
        public DateTime LastTransferTime => new(Interlocked.Read(ref _lastTransferTimeTicks), DateTimeKind.Utc);

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
            Connection = connection;
            Reader = reader;
            PacketSender = packetSender;

            Events = new EventController(eventFactory);
            OutboundChannel = Channel.CreateBounded<Packet>(
                new BoundedChannelOptions(channelCapacity > 0 ? channelCapacity : 1000)
                {
                    FullMode = BoundedChannelFullMode.Wait
                }
            );
            CollectingStats = collectingStats;
            RunProcessing(token);
        }

        public void RunProcessing(CancellationToken token)
        {
            if (IsRunning)
            {
                return;
            }
            IsRunning = true;
            _ = Task.Run(() => OutboundChannelWorker(token), token);  // Each session has its own packet sender
        }

        private async Task OutboundChannelWorker(CancellationToken token)
        {
            if (!IsRunning)
            {
                return;
            }

            try
            {
                await foreach (Packet packet in OutboundChannel.Reader.ReadAllAsync(token))
                {
                    await _streamLocker.WaitAsync(token);
                    try
                    {
                        await PacketSender.SendPacketAsync(Connection.GetNetworkStream(), packet, token);
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
                while (OutboundChannel.Reader.TryRead(out Packet? packet))
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
            return Reader.ReadHeaderAsync(Connection.GetNetworkStream(), token);
        }

        public Task<byte[]> ReadPayloadAsync(int length, CancellationToken token)
        {
            return Reader.ReadPayloadAsync(Connection.GetNetworkStream(), length, token);
        }

        public void SendPacket(Packet packet)
        {
            if (!IsRunning)
            {
                if (packet is IReleasable releasable)
                {
                    releasable.Release();
                }
                return;
            }

            if (OutboundChannel.Writer.TryWrite(packet))
            {
                return;
            }

            OutboundChannel.Reader.TryRead(out Packet? oldestPacket);
            if (oldestPacket != null && oldestPacket is IReleasable releasableOldest)
            {
                releasableOldest.Release();
            }

            if (!OutboundChannel.Writer.TryWrite(packet) && packet is IReleasable releasableDuplication)
            {
                releasableDuplication.Release();
            }
        }

        public void StartEvent(string eventName, Dictionary<string, object> eventSettings)
        {
            Events.StartEvent(this, eventName, eventSettings);
        }

        public void IncrementSendPackets()
        {
            if (CollectingStats)
            {
                Interlocked.Increment(ref _totalPacketsSent);
                Interlocked.Exchange(ref _lastTransferTimeTicks, DateTime.UtcNow.Ticks);
            }
        }

        public void IncrementReceivedPackets()
        {
            if (CollectingStats)
            {
                Interlocked.Increment(ref _totalPacketsReceived);
                Interlocked.Exchange(ref _lastTransferTimeTicks, DateTime.UtcNow.Ticks);
            }
        }

        public void StopProcessing()
        {
            IsRunning = false;
            OutboundChannel.Writer.TryComplete();
            Events.StopAllRunning();
            Connection.Close();
        }
    }
}
