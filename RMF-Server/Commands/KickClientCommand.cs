using Microsoft.Extensions.Logging;
using RMF.Core.Network;
using RMF_Server.Configurations;
using RMF_Server.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal sealed class KickClientCommand : InlineCommand
    {
        private readonly IServerSessionManager _sessionManager;

        public override string Category => "Management";
        public override string Name => "kick";
        public override string Description => "Disconnects the specified session from the server";
        public override string[]? Parameters => ["ip:port"];

        public KickClientCommand(
            IServerSessionManager sessionManager,
            ILogger<CommandDispatcher> cmLogger,
            CommandConfig commandConfig
        ) : base(cmLogger, commandConfig)
        {
            _sessionManager = sessionManager;
        }

        public override Task ExecuteAsync(string[] args, CancellationToken token)
        {
            if (args.Length != 1)
            {
                CmLogger.LogError("Fill in the kick command according to the structure shown below:\n{Syntax}", ToString());
                return Task.CompletedTask;
            }

            string targetEndPoint = args[0];
            if (_sessionManager.GetClientSession(targetEndPoint, out _))
            {
                _sessionManager.Disconnect(targetEndPoint);
                CmLogger.LogInformation("Successfully disconnected {EndPoint}", targetEndPoint);
            }
            else
            {
                CmLogger.LogError("No connection found named \"{EndPoint}\"", targetEndPoint);
            }
            return Task.CompletedTask;
        }
    }
}
