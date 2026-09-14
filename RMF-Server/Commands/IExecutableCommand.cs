using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal interface IExecutableCommand
    {
        string Category { get; }
        string Name { get; }
        string Description { get; }
        string[]? Parameters { get; }

        Task ExecuteAsync(string[] args, CancellationToken token);
    }
}
