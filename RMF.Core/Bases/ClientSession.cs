using RMF.Core.Events;
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
        public TcpClient Client { get; }
        public IPEndPoint? EndPoint => this.Client.Client.RemoteEndPoint as IPEndPoint;
        protected NetworkStream? Stream => this.Client.Connected ? this.Client.GetStream() : null;

        public EventController Events { get; private set; } = new();
        protected Channel<Packet> OutboundChannel { get; private set; }
        public bool IsRunning { get; private set; }

        public ClientSession(TcpClient client, CancellationToken token)
        {
            this.Client = client;
            this.OutboundChannel = Channel.CreateUnbounded<Packet>();

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

            await foreach (Packet packet in this.OutboundChannel.Reader.ReadAllAsync(token))
            {
                await StreamManager.SendPacketAsync(this.Client.GetStream(), packet, token);
                if (packet is IReleasable releasable)
                {
                    releasable.Release();
                }
            }
        }

        public void SendPacket(Packet packet)
        {
            if (this.IsRunning)
            {
                this.OutboundChannel.Writer.TryWrite(packet);
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
    }
}
