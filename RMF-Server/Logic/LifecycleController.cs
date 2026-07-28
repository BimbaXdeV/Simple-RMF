using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Network;
using RMF.Core.Packets.Server;
using RMF_Server.Channels;
using RMF_Server.Configurations;
using RMF_Server.Debugger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal class LifecycleController
    {
        private readonly IServerSessionManager _sessionManager;
        private readonly IChannelDispatcher _channelDispatcher;
        private readonly ILoggingEngine _logger;
        private readonly ControllerConfig _controllerConfig;

        public bool IsBaseInitialized;
        public CancellationTokenSource? Input { get; private set; }  // Master token source, it starts the whole chain of shutdown
        public CancellationTokenSource? Server { get; private set; }

        public bool IsFinalInitialized;
        public CancellationTokenSource? Output { get; private set; }

        public LifecycleController(
            IServerSessionManager sessionManager,
            IChannelDispatcher channelDispatcher,
            ILoggingEngine logger,
            ControllerConfig controllerConfig
        )
        {
            this._sessionManager = sessionManager;
            this._channelDispatcher = channelDispatcher;
            this._logger = logger;
            this._controllerConfig = controllerConfig;

            this.IsBaseInitialized = false;
            this.IsFinalInitialized = false;
        }

        public void Initialize()
        {
            if (this.IsBaseInitialized && this.IsFinalInitialized)
            {
                return;
            }

            PropertyInfo[] tokenSources = typeof(LifecycleController).GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(CancellationTokenSource))
                .ToArray();

            foreach (PropertyInfo property in tokenSources)
            {
                CancellationTokenSource cts = new();
                property.SetValue(null, cts);
            }
            this.IsBaseInitialized = true;
            this.IsFinalInitialized = true;
        }

        private async Task WaitForParting(int timeoutSecs)
        {
            this._logger.Output("The server is parting...");
            DateTime deadline = DateTime.Now + TimeSpan.FromSeconds(timeoutSecs);
            while (this._sessionManager != null &&
                this._sessionManager.ConnectionsExist &&
                DateTime.Now < deadline)
            {
                await Task.Delay(100);
            }

            if (this._sessionManager?.ConnectionsExist == true)
            {
                this._logger.Warning($"The server parting timeout has expired, {this._sessionManager.TotalConnections} clients are still connected");
            }
            this._logger.Output("The server successfully parted");
        }

        public async Task BaseShutdown()
        {
            if (!this.IsBaseInitialized)
            {
                this._logger.Warning("The server lifecycle is not initialized, shutdown is not required");
                return;
            }

            this._logger.Separator();
            this._logger.Warning("Cancellation requested, stopping server...");

            this.Input!.Cancel();

            if (this._controllerConfig.EnableRelativeParting && this._sessionManager != null)
            {
                EndOfEventsRequest endOfEventsRequest = new();
                this._sessionManager.BroadcastPacket(endOfEventsRequest, CancellationToken.None);
                await WaitForParting(this._controllerConfig.PartingTimeoutSecs);
            }

            this.Server!.Cancel();
            this._sessionManager?.ClearConnections();

            if (this._channelDispatcher != null)
            {
                await this._channelDispatcher.CloseChannels();
            }

            this.IsBaseInitialized = false;
        }

        public void FinalShutdown(bool cleanupSources = false)
        {
            if (!this.IsFinalInitialized)
            {
                this._logger.Warning("The final lifecycle is not initialized, shutdown is not required");
                return;
            }

            this.Output!.Cancel();
            if (cleanupSources)
            {
                DisposeAll();
            }
            this.IsFinalInitialized = false;
        }

        public void DisposeAll()
        {
            PropertyInfo[] tokenSources = typeof(LifecycleController).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(CancellationTokenSource))
                .ToArray();

            int disposedSourcesCount = 0;
            int totalTokenSources = tokenSources.Length;
            foreach (PropertyInfo property in tokenSources)
            {
                CancellationTokenSource? cts = property.GetValue(null) as CancellationTokenSource;
                if (cts != null && cts is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                property.SetValue(null, null);
                disposedSourcesCount++;
            }
            this._logger.Output($"During the token cleanup, {disposedSourcesCount} / {totalTokenSources} active sources were cleared successfully");
        }
    }
}
