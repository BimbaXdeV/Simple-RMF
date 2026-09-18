using Microsoft.Extensions.Logging;
using RMF_Server.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal class CommandDispatcher : ICommandRouter, ICommandViewer
    {
        private readonly Dictionary<string, IExecutableCommand> _commands;
        private readonly ILogger<CommandDispatcher> _logger;
        private readonly CommandConfig _commandConfig;

        public CommandDispatcher(
            IEnumerable<IExecutableCommand> commands,
            ILogger<CommandDispatcher> logger,
            CommandConfig commandConfig
        )
        {
            _commandConfig = commandConfig;
            _commands = commands.ToDictionary(
                c => c.Name,
                c => c
            );
            _logger = logger;
        }

        public async Task RouteCommandAsync(string commandName, string[] commandArgs, CancellationToken token)
        {
            Console.WriteLine($"Routing command: {commandName} with args: {string.Join(", ", commandArgs)}");
            if (_commands.TryGetValue(commandName, out IExecutableCommand? command))
            {
                await command.ExecuteAsync(commandArgs, token);
            }
            else
            {
                _logger.LogError(
                    "Unknown command: \"{CommandName}\". Type \"{CommandSign}help\" to see all available inline commands",
                    commandName,
                    _commandConfig.InlineCommandDefautSign
                );
            }
        }

        public IExecutableCommand[] GetLoadedCommands()
        {
            return _commands.Values.ToArray();
        }

        public int GetLoadedCommandsCount()
        {
            return _commands.Count;
        }

        public IExecutableCommand? FindSimilarCommand(string commandHeaderPart)
        {
            return _commands.Values.FirstOrDefault(
                c => c.Name.StartsWith(commandHeaderPart, StringComparison.OrdinalIgnoreCase)
            );
        }
    }
}
