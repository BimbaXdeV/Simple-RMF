using Avalonia.Platform;
using Avalonia.Threading;
using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Logic;
using RMF.Core.Interfaces.Network;
using RMF_Server.Configurations;
using RMF_Server.Debugger;
using RMF_Server.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal class AppearanceManager : IWindowManager, IDisposable
    {
        private readonly IServerSessionManager _sessionManager;
        private readonly ILoggingEngine _logger;
        private readonly AppearanceConfig _appearanceConfig;

        private const byte MaxTitleLength = 48;

        public AppearanceManager(
            IServerSessionManager sessionManager,
            ILoggingEngine logger,
            AppearanceConfig appearanceConfig
        )
        {
            this._sessionManager = sessionManager;
            this._logger = logger;
            this._appearanceConfig = appearanceConfig;

            UpdateTitleOnline(this._sessionManager.TotalConnections);
            this._sessionManager.ConnectionCountChanged += OnConnectionCountChanged;
        }

        private void OnConnectionCountChanged(int newConnectionCount)
        {
            UpdateTitleOnline(newConnectionCount);
        }

        public void UpdateTitleOnline(int connectionCount)
        {
            int titleHeaderLength = this._appearanceConfig.AppTitle.Length + 11;  // "<Title> | Online: "
            if (connectionCount <= 0 || titleHeaderLength + connectionCount.ToString().Length > MaxTitleLength)
            {
                this._logger.Warning($"Failed to update application title, received too long string (max length: {MaxTitleLength})");
                return;
            }

            Console.Title = $"{this._appearanceConfig.AppTitle} | Online: {connectionCount}";
        }

        public void Dispose()
        {
            this._sessionManager.ConnectionCountChanged -= OnConnectionCountChanged;
        }
    }
}
