using Microsoft.Extensions.Logging;
using RMF.Core.Network;
using RMF.Core.Security;
using RMF_Server.Configurations;
using RMF_Server.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal sealed class BanClientCommand : InlineCommand
    {
        private readonly IServerSessionManager _sessionManager;
        private readonly IFirewall _firewall;

        public override string Category => "Management";
        public override string Name => "ban";
        public override string Description => "Disconnects the client from the server and subsequently bans them from connecting";
        public override string[]? Parameters => ["ip:port"];

        public BanClientCommand(
            IServerSessionManager sessionManager,
            IFirewall firewall,
            ILogger<CommandDispatcher> cmLogger,
            CommandConfig commandConfig
        ) : base(cmLogger, commandConfig)
        {
            _sessionManager = sessionManager;
            _firewall = firewall;
        }

        public override Task ExecuteAsync(string[] args, CancellationToken token)
        {
            if (args.Length != 1)
            {
                CmLogger.LogError("Fill in the ban command according to the structure shown below:\n{Syntax}", ToString());
                return Task.CompletedTask;
            }

            string targetIp = args[0];
            _firewall.Ban(targetIp);

            if (_sessionManager.GetClientSession(targetIp, out _))
            {
                _sessionManager.Disconnect(targetIp);
            }
            return Task.CompletedTask;
        }
    }
}
