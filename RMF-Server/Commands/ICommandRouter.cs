using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal interface ICommandRouter
    {
        Task RouteCommandAsync(string commandHeader, string[] commandArgs, CancellationToken token);
    }
}
