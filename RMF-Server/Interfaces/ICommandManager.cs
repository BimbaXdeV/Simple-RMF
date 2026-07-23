using RMF_Server.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Interfaces
{
    internal interface ICommandManager
    {
        Command? GetCommand(string name);
        List<Command> GetAllCommands();
        Command? GetSimilarityCommand(string name);
    }
}
