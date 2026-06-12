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
            (int colorsLoaded, int totalColors) = ThemeManager.Load();
            Logging.Message(Logging.ServerLogo, toHistory: false);
            Logging.Separator();

            Logging.Output("Initializing components...");
            LifecycleController.Initialize();

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

            // Already loaded colors
            Logging.Message($"Theme colors:         {colorsLoaded} / {totalColors}", leftOffset: Logging.LogHeaderLength);


            // Transferring fields data from server configurations to core packet configurations
            SettingsSynchronizer.Upload(typeof(ConfigurationManager), typeof(PacketConfigurations));

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                LifecycleController.Input!.Cancel();
            };

            Logging.CreateHistory(ConfigurationManager.LoggingHistoryLength);

            Task loggingTask = Logging.RunExecutor(LifecycleController.Output!.Token);
            Task serverTask = new OpenTCP().RunServer(LifecycleController.Server!.Token);

            Task activeLogic = Task.Run(async () =>
            {
                await WindowManager.WaitForUIReady();
                Logging.Output("UI Thread successfully initialized");
                
                try
                {
                    await InputListener.StartListen(LifecycleController.Input!);
                }
                finally
                {
                    // Stopping the server listener, closing sockets, and cleaning up client sessions
                    await LifecycleController.BaseShutdown();

                    // Terminate the blocking Avalonia Application
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        (Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
                    });
                }
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

            await Task.WhenAll(activeLogic, serverTask).ConfigureAwait(false);
            LifecycleController.FinalShutdown();
            await loggingTask.ConfigureAwait(false);

            Logging.Output("Cleaning up resources...");
            LifecycleController.DisposeAll();
            Logging.Output("The work process is completed. Goodbye!");

            // Saving logs to file if enabled, with an option to append to existing backup or create a new one
            if (ConfigurationManager.EnableLogSaving)
            {
                Logging.SaveBackup(
                    PathManager.GetResolvedPath("ActualLog", fileName: "rmf-server", fileFormat: "log"),
                    appendBelow: ConfigurationManager.EnableMultipleBackup
                );
            }

            // If you really want to read what is written during a cascade shutdown :)
            if (!ConfigurationManager.EnableForceShutdown)
            {
                Logging.Output("To finish this process, press any key...");
                Console.ReadKey();
            }
        }
    }
}
