using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal interface ICommandHandler
    {
        void SwitchHandle(string command);
        Task SearchHandle(string input, Command command, CancellationToken token);
    }
}
