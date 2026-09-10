using Microsoft.Extensions.Logging;
using RMF.Core.Security;
using RMF_Server.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal sealed class BlacklistCommand : InlineCommand
    {
        private readonly IFirewall _firewall;

        public override string Category => "Performance";
        public override string Name => "blacklist";
        public override string Description => "Displays a list of IP addresses blocked by the server";
        public override string[]? Parameters => null;

        public BlacklistCommand(
            IFirewall firewall,
            ILogger<CommandHandler> cmLogger,
            CommandConfig commandConfig
        ) : base(cmLogger, commandConfig)
        {
            _firewall = firewall;
        }

        public override void Execute(string[] args)
        {
            string[] bannedIPs = _firewall.GetBannedIPs();
            if (bannedIPs.Length == 0)
            {
                CmLogger.LogInformation("No banned IPs...");
                return;
            }

            CmLogger.LogInformation("Server blacklist:");
            int maxCounterLength = bannedIPs.Length.ToString().Length;
            int counter = 1;
            foreach (string ip in bannedIPs)
            {
                CmLogger.LogInformation("{Index}. {IpAddress}", counter.ToString().PadLeft(maxCounterLength), ip);
                counter++;
            }
        }
    }
}
