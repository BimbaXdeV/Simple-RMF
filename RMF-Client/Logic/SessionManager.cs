using Microsoft.Extensions.Hosting;
using RMF.Core.Events;
using RMF.Core.Interfaces;
using RMF.Core.Network;
using RMF_Client.Configurations;
using RMF_Client.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Logic
{
    internal class SessionManager : IClientSessionManager
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IProtocolReader _protocolReader;
        private readonly IPacketSender _packetSender;
        private readonly IEventFactory _eventFactory;
        private readonly ControllerConfig _controllerConfig;
        private readonly ChannelConfig _channelConfig;

        private IConnectionClientSession? _session;

        public bool IsConnected => this._session?.IsRunning ?? false;

        public SessionManager(
            IHostApplicationLifetime lifetime,
            IProtocolReader protocolReader,
            IPacketSender packetSender,
            IEventFactory eventFactory,
            ControllerConfig controllerConfig,
            ChannelConfig channelConfig
        )
        {
            this._lifetime = lifetime;
            this._protocolReader = protocolReader;
            this._packetSender = packetSender;
            this._eventFactory = eventFactory;
            this._controllerConfig = controllerConfig;
            this._channelConfig = channelConfig;
        }

        public void StartSession(INetworkConnection connection)
        {
            try
            {
                IConnectionClientSession session = new ConnectionClientSession(
                    connection,
                    this._protocolReader,
                    this._packetSender,
                    this._eventFactory,
                    channelCapacity: this._channelConfig.ChannelPacketsCapacity,
                    collectingStats: this._controllerConfig.EnableCollectingSessionStats,
                    token: this._lifetime.ApplicationStopping
                );
                this._session = session;
            }
            catch (Exception)
            {
            }
        }

        public IConnectionClientSession? GetRunningSession()
        {
            return this._session;
        }

        public void StopSession()
        {
            this._session?.StopProcessing();
            this._session = null;
        }
    }
}
