using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using RMF.Core.Events;
using RMF.Core.Packets;
using RMF_Server.Channels;
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
        [STAThread]
        static async Task Main(string[] args)
        {
            AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle}  |  Offline");
            Logging.Message(Logging.ServerLogo, toHistory: false);
            Logging.Separator();

            Logging.Output("Initializing components...");
            
            (int configurationsLoaded, int totalConfigurations) = ConfigurationManager.Load();
            Logging.Message($"Configuration fields: {configurationsLoaded} / {totalConfigurations}", leftOffset: Logging.LogHeaderLength);

            (int commandsLoaded, int totalCommands) = CommandManager.Load();
            Logging.Message($"Inline commands:      {commandsLoaded} / {totalCommands}", leftOffset: Logging.LogHeaderLength);

            (int packetsLoaded, int totalPackets) = PacketsAssembler.RegisterFound();
            Logging.Message($"Network packets:      {packetsLoaded} / {totalPackets}", leftOffset: Logging.LogHeaderLength);

            (int pathsLoaded, int totalPaths) = PathManager.Load();
            Logging.Message($"External paths:       {pathsLoaded} / {totalPaths}", leftOffset: Logging.LogHeaderLength);

            (int channelsLoaded, int totalChannels) = ChannelDispatcher.StartFound();
            Logging.Message($"Process channels:     {channelsLoaded} / {totalChannels}", leftOffset: Logging.LogHeaderLength);

            (int eventsLoaded, int totalEvents) = EventAssembler.RegisterFound("Server");
            Logging.Message($"Server events:        {eventsLoaded} / {totalEvents}", leftOffset: Logging.LogHeaderLength);


            // Transferring fields data from server configurations to core packet configurations
            SettingsSynchronizer.Upload(typeof(ConfigurationManager), typeof(PacketConfigurations));

            using CancellationTokenSource cts = new();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            Logging.CreateHistory(ConfigurationManager.LoggingHistoryLength);

            Task loggingTask = Logging.RunExecutor(cts.Token);
            OpenTCP tcp = new();
            Task serverTask = tcp.RunServer(cts.Token);

            Task activeLogic = Task.Run(async () =>
            {
                await WindowManager.WaitForUIReady();
                try
                {
                    await InputListener.StartListen(cts);
                }
                catch (OperationCanceledException)
                {
                }

                cts.Cancel();
                await Task.WhenAll(serverTask, loggingTask);

                Dispatcher.UIThread.Post(() =>
                {
                    (Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
                });
            });

            WindowManager.BuildAvaloniaApp()
                         .AfterSetup(_ =>
                         {
                             Dispatcher.UIThread.Post(() => WindowManager.UIInitSource.TrySetResult());
                         })
                         .StartWithClassicDesktopLifetime(
                             args: [],
                             shutdownMode: Avalonia.Controls.ShutdownMode.OnExplicitShutdown
                         );
            WindowManager.ShowWindow();

            await activeLogic;
            Logging.Output("The work process is completed. Goodbye!");
        }
    }
}
