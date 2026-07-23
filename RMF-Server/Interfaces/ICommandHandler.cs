using RMF_Server.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Interfaces
{
    internal interface ICommandHandler
    {
        void SwitchHandle(string command);
        Task SearchHandle(string input, Command command, CancellationTokenSource cts);
    }
}
