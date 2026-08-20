using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<AppearanceManager> _logger;
        private readonly AppearanceConfig _appearanceConfig;

        private const string _titleStatusHeader = "Online: ";
        private const byte _maxTitleLength = 48;

        public AppearanceManager(
            IServerSessionManager sessionManager,
            ILogger<AppearanceManager> logger,
            AppearanceConfig appearanceConfig
        )
        {
            this._sessionManager = sessionManager;
            this._logger = logger;
            this._appearanceConfig = appearanceConfig;

            UpdateTitleStatus(_titleStatusHeader + this._sessionManager.TotalConnections);
            this._sessionManager.ConnectionCountChanged += OnConnectionCountChanged;
        }

        private void OnConnectionCountChanged(int newConnectionCount)
        {
            UpdateTitleStatus(_titleStatusHeader + newConnectionCount);
        }

        public void UpdateTitleStatus(string newStatus)
        {
            int titleHeaderLength = this._appearanceConfig.AppTitle.Length + 11;  // "<Title> | Online: "
            if (newStatus.Length <= 0 || titleHeaderLength + newStatus.Length > _maxTitleLength)
            {
                this._logger.LogWarning("Failed to update application title, received too long string (max length: {MaxTitleLength})", _maxTitleLength);
                return;
            }

            Console.Title = this._appearanceConfig.AppTitle + " | " + newStatus;
        }

        public void Dispose()
        {
            this._sessionManager.ConnectionCountChanged -= OnConnectionCountChanged;
        }
    }
}
