using Microsoft.Extensions.Logging;
using RMF_Server.Configurations;
using RMF_Server.Debugger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal sealed class ClearCommand : InlineCommand
    {
        private readonly IConsoleExtensions _consoleExtensions;

        public override string Category => "Performance";
        public override string Name => "clear";
        public override string Description => "Clears the text displayed in the console";
        public override string[]? Parameters => null;

        public ClearCommand(
            IConsoleExtensions consoleExtensions,
            ILogger<CommandHandler> cmLogger,
            CommandConfig commandConfig
        ) : base(cmLogger, commandConfig)
        {
            _consoleExtensions = consoleExtensions;
        }

        public override void Execute(string[] args)
        {
            _consoleExtensions.ClearConsole(CmLogger);
        }
    }
}
