using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal interface ICommandManager
    {
        Command? GetCommand(string name);
        List<Command> GetAllCommands();
        Command? GetSimilarityCommand(string name);
    }
}
