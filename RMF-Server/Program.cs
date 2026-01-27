using RMF_Server.Commands;
using RMF_Server.Debugger;
using RMF_Server.Logic;
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
            CommandManager.Load();

            using CancellationTokenSource cts = new();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            Task loggingTask = Logging.RunExecutor(cts.Token);
            OpenTCP tcp = new OpenTCP();
            Task serverTask = tcp.RunServer(cts.Token);

            try
            {
                await InputListener.StartListen(cts);
            }
            catch (OperationCanceledException)
            {
            }

            cts.Cancel();
            await Task.WhenAll(loggingTask, serverTask);
            Logging.Output("The work process is completed. Goodbye!");
        }
    }
}
