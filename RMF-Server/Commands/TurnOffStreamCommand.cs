using Microsoft.Extensions.Logging;
using RMF.Core.Network;
using RMF.Core.Packets.Server;
using RMF_Server.Configurations;
using RMF_Server.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal sealed class TurnOffStreamCommand : InlineCommand
    {
        private readonly IAvaloniaManager _avaloniaManager;
        private readonly IServerSessionManager _sessionManager;
        private readonly AppearanceConfig _appearanceConfig;

        public override string Category => "Management";
        public override string Name => "streamoff";
        public override string Description => "Notifies the client of the completion of the streaming stream";
        public override string[]? Parameters => null;

        public TurnOffStreamCommand(
            IAvaloniaManager avaloniaManager,
            IServerSessionManager sessionManager,
            ILogger<CommandDispatcher> cmLogger,
            AppearanceConfig appearanceConfig,
            CommandConfig commandConfig
        ) : base(cmLogger, commandConfig)
        {
            _avaloniaManager = avaloniaManager;
            _sessionManager = sessionManager;
            _appearanceConfig = appearanceConfig;
        }

        public override async Task ExecuteAsync(string[] args, CancellationToken token)
        {
            try
            {
                IPEndPoint? ipEndPoint = _avaloniaManager.StreamingClientEndPoint;
                if (ipEndPoint == null)
                {
                    CmLogger.LogInformation("No active stream to stop...");
                    return;
                }

                string endPoint = ipEndPoint.ToString();
                if (_sessionManager.GetClientSession(endPoint, out IServerClientSession? session))
                {
                    StreamingRequest streamingRequest = new()
                    {
                        IsActive = false
                    };
                    session!.SendPacket(streamingRequest);
                    CmLogger.LogInformation("Successfully sent to {EndPoint}, waiting for stopping stream...", endPoint);
                }
                else
                {
                    CmLogger.LogError("No connection found named \"{EndPoint}\"", endPoint);
                }
            }
            finally
            {
                _avaloniaManager.SetWindowTitle(_appearanceConfig.WindowTitle);
                await _avaloniaManager.HideWindow();
            }
        }
    }
}
