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
            using CancellationTokenSource cts = new();

            Task loggingTask = Logging.RunExecutor();
            Logging.Output("Starting Server...");
            ConfigurationManager.Load();

            OpenTCP tcp = new OpenTCP();
            Task serverTask = tcp.RunServer(cts.Token);

            cts.Cancel();

            await Task.WhenAll(loggingTask, serverTask);

            //CancellationTokenSource cts = new CancellationTokenSource();
            //Console.CancelKeyPress += (s, e) =>
            //{
            //    e.Cancel = true;
            //    cts.Cancel();
            //};
        }
    }
}
