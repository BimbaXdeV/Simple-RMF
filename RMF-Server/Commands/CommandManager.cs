using RMF.Core.Interfaces;
using RMF_Server.Debugger;
using RMF_Server.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF_Server.Commands
{
    internal class CommandManager : ICommandManager
    {
        private readonly List<InlineCommand> _commands;

        public CommandManager(List<InlineCommand>? commands)
        {
            this._commands = commands ?? [];
        }

        public InlineCommand? GetCommand(string name)
        {
            return this._commands.FirstOrDefault(c => c.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
        }

        public List<InlineCommand> GetAllCommands()
        {
            return this._commands;
        }

        public InlineCommand? GetSimilarityCommand(string name)
        {
            return this._commands.FirstOrDefault(c => c.Name != null && c.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
