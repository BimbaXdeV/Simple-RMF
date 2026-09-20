using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMF_Server.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal sealed class ShutdownCommand : InlineCommand
    {
        private readonly IHostApplicationLifetime _lifetime;

        public override string Category => "Performance";
        public override string Name => "shutdown";
        public override string Description => "Cancels the main lifecycle token, terminating the server process (just Ctrl+C, bro)";
        public override string[]? Parameters => null;

        public ShutdownCommand(
            IHostApplicationLifetime lifetime,
            ILogger<CommandDispatcher> cmLogger,
            CommandConfig commandConfig
        ) : base(cmLogger, commandConfig)
        {
            _lifetime = lifetime;
        }

        public override Task ExecuteAsync(string[] args, CancellationToken token)
        {
            CmLogger.LogInformation("The shutdown command received. Initiating cancellation process...");
            _lifetime.StopApplication();
            return Task.CompletedTask;
        }
    }
}
