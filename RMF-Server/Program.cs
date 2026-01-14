using RMF_Server.Debugger;
using RMF_Server.Logic;
using RMF_Server.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Logging.Output("Starting Server...");
            ConfigurationManager.Load();
            OpenTCP tcp = new OpenTCP();
            
            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };
            
            await tcp.RunServer(cts.Token);
        }
    }
}
