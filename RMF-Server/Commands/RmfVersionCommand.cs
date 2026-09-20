using Microsoft.Extensions.Logging;
using RMF_Server.Configurations;
using RMF_Server.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal sealed class RmfVersionCommand : InlineCommand
    {
        private readonly AppearanceConfig _appearanceConfig;

        public override string Category => "Performance";
        public override string Name => "version";
        public override string Description => "Displays the versions of the current server build and the RMF.Core separately";
        public override string[]? Parameters => null;

        public RmfVersionCommand(
            ILogger<CommandDispatcher> cmLogger,
            AppearanceConfig appearanceConfig,
            CommandConfig commandConfig
        ) : base(cmLogger, commandConfig)
        {
            _appearanceConfig = appearanceConfig;
        }

        public override Task ExecuteAsync(string[] args, CancellationToken token)
        {
            string? serverVersion = RmfVersion.App?.ToString(3);
            string? coreVersion = RmfVersion.Core?.ToString(3);

            if (serverVersion != null && coreVersion != null)
            {
                CmLogger.LogInformation("Assembly versions:");
                CmLogger.LogInformation("{ServerName}: {ServerVersion}", _appearanceConfig.AppTitle, serverVersion);
                CmLogger.LogInformation("RMF.Core: {CoreVersion}", coreVersion);
            }
            else
            {
                CmLogger.LogWarning("Version information is not available now");
            }
            return Task.CompletedTask;
        }
    }
}
