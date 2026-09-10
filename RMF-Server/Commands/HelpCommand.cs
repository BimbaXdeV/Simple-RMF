using Microsoft.Extensions.Logging;
using RMF_Server.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal sealed class HelpCommand : InlineCommand
    {
        public override string Category => "Performance";
        public override string Name => "help";
        public override string Description => "Displays a list of available inline commands and their descriptions";
        public override string[]? Parameters => null;

        public HelpCommand(ILogger<CommandHandler> cmLogger, CommandConfig commandConfig) : base(cmLogger, commandConfig)
        {
        }

        public override void Execute(string[] args)
        {
        }
    }
}
