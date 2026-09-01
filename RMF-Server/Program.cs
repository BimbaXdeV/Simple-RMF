using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMF.Core.Loaders;
using RMF_Server.Commands;
using RMF_Server.Configurations;
using RMF_Server.Debugger;
using RMF_Server.DI;
using RMF_Server.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            RmfServerHost server = new(args);
            await server.RunAsync(args);
        }
    }
}
