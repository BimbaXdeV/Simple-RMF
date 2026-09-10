using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal interface ICommandManager
    {
        InlineCommand? GetCommand(string name);
        List<InlineCommand> GetAllCommands();
        InlineCommand? GetSimilarityCommand(string name);
    }
}
