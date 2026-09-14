using Microsoft.Extensions.Logging;
using RMF_Server.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal abstract class InlineCommand : IExecutableCommand
    {
        protected readonly ILogger<CommandDispatcher> CmLogger;
        protected readonly CommandConfig CommandConfig;

        public abstract string Category { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string[]? Parameters { get; }

        protected InlineCommand(ILogger<CommandDispatcher> linkedLogger, CommandConfig commandConfig)
        {
            CmLogger = linkedLogger;
            CommandConfig = commandConfig;
        }

        public abstract Task ExecuteAsync(string[] args, CancellationToken token);

        public override string ToString()
        {
            char commandPref = CommandConfig.InlineCommandDefautSign;
            if (Parameters == null || Parameters.Length <= 0)
            {
                return commandPref + Name;
            }

            IEnumerable<string> formattedParameters = Parameters.Select(p => '[' + p + ']');
            return commandPref + Name + ' ' + string.Join(' ', formattedParameters);
        }
    }
}
