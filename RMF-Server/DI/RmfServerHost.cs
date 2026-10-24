using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMF.Core.Appearance;
using RMF.Core.Events;
using RMF.Core.Extensions;
using RMF.Core.Loaders;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF.Core.Security;
using RMF_Server.Channels;
using RMF_Server.Commands;
using RMF_Server.Configurations;
using RMF_Server.Debugger;
using RMF_Server.Logic;
using RMF_Server.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.DI
{
    internal class RmfServerHost
    {
        private readonly IHost _host;
        private readonly ILogger _bootLogger;

        public RmfServerHost(string[] args)
        {
            // ---- Part of the server component initialization ----
            // All necessary components must be initialized before the DI container is built,
            // which should save resources during server startup;

            // [!] Ultimately, the decision was made to completely abandon partial resource loading
            // and instead throw an exception immediately if external files or parsing systems failed

            // Logging dependencies (color theme + synchronizer + config)
            (Dictionary<Type, object> serverConfigurations, int loadedConfigFields) = XmlConfigLoader.Load(Path.Combine("Resources", "config.xml"));
            XmlConfigProvider configProvider = new(serverConfigurations, loadedConfigFields);
            LoggingConfig loggingConfig = configProvider.GetConfig<LoggingConfig>();

            // Console theme
            Dictionary<string, ThemeColor> serverTheme = XmlThemeLoader.Load(Path.Combine("Resources", "theme.xml"));
            ThemeManager themeManager = new(serverTheme, new ThemeColor(255, 255, 255));

            // ---- Registering the logger provider and initial console output ----
            ConsoleSynchronizer consoleSynchronizer = new();
            RmfLoggerProvider loggerProvider = new(themeManager, consoleSynchronizer, loggingConfig);
            RmfConsoleAppearance consoleAppearance = new(themeManager, loggingConfig);
            _bootLogger = loggerProvider.CreateLogger("RmfServerBoot");

            consoleAppearance.DrawLogo(_bootLogger);
            consoleAppearance.LogSeparator(_bootLogger);
            _bootLogger.LogInformation("Preparing to launch the server:");

            // ---- Assembling services into a DI container ----
            IHostBuilder builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                // Logging dependencies implementation
                services.AddSingleton<IThemeManager>(themeManager);
                services.AddSingleton<IConsoleSynchronizer>(consoleSynchronizer);
                services.AddSingleton(loggingConfig);

                // Logging provider & extensions implementation + background executor
                services.AddSingleton(loggerProvider);
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.Services.AddSingleton<ILoggerProvider>(loggerProvider);
                });
                services.AddHostedService(provider => provider.GetRequiredService<RmfLoggerProvider>());
                services.AddSingleton<IConsoleExtensions>(consoleAppearance);

                // Configurations
                services.AddSingleton<IConfigViewer>(configProvider);
                services.AddSingleton(configProvider);
                services.AddSingletonConfigurations();

                // Sessions & Network
                services.AddSingleton<IProtocolReader, ProtocolReader>(provider =>
                {
                    FirewallConfig firewallConfig = provider.GetRequiredService<FirewallConfig>();
                    return new ProtocolReader(firewallConfig.MaxPacketBufferKB * 1024);
                });
                services.AddSingleton<IPacketSender, StreamManager>();
                services.AddSingleton<IEventFactory, EventFactory>();
                services.AddSingleton<IServerSessionManager, SessionManager>();

                // UI
                services.AddSingleton<IAvaloniaManager, AvaloniaManager>();
                services.AddSingleton<AppearanceManager>();
                services.AddSingleton<IWindowManager>(provider => provider.GetRequiredService<AppearanceManager>());
                services.AddHostedService(provider => provider.GetRequiredService<AppearanceManager>());

                // Metrics onitoring
                services.AddSingleton<IServerMetricsMonitor, RmfServerMetrics>();

                // Packets
                services.AddSingleton<IPacketFactory, PacketFactory>();
                services.AddSingleton<IServerPacketProcessor, PacketProcessor>();

                // Channels
                services.AddSingleton<IChannelDispatcher, ChannelDispatcher>();
                services.AddHostedService(provider => (ChannelDispatcher)provider.GetRequiredService<IChannelDispatcher>());

                // Commands
                services.AddSingletonInlineCommands();
                services.AddSingleton<CommandDispatcher>();
                services.AddSingleton<ICommandRouter, CommandDispatcher>(provider => provider.GetRequiredService<CommandDispatcher>());
                services.AddSingleton<ICommandViewer, CommandDispatcher>(provider => provider.GetRequiredService<CommandDispatcher>());

                // Server dependencies
                services.AddSingleton<IConnectionListener, TcpListenerAdapter>(provider =>
                {
                    ConnectionConfig connectionConfig = provider.GetRequiredService<ConnectionConfig>();

                    IPAddress ip = connectionConfig.IPAddress;
                    int port = connectionConfig.Port;

                    TcpListener listener = new(ip, port);
                    return new TcpListenerAdapter(listener);
                });
                services.AddSingleton<ITlsManager, TlsManager>();
                services.AddSingleton<IFirewall, Firewall>();

                // Server engine services
                services.AddHostedService<ServerBootstrapper>();
                services.AddHostedService<NetworkEngine>();
                services.AddHostedService<InputListener>();
            });

            _host = builder.Build();
        }

        [STAThread]
        public async Task RunAsync(string[] args)
        {
            IHostApplicationLifetime lifetime = _host.Services.GetRequiredService<IHostApplicationLifetime>();
            IAvaloniaManager avaloniaManager = _host.Services.GetRequiredService<IAvaloniaManager>();

            lifetime.ApplicationStopping.Register(() =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        desktopLifetime.Shutdown();
                        _bootLogger.LogInformation("Avalonia graphics thread has been successfully stopped");
                    });
                }
            });

            await _host.StartAsync();

            try
            {
                avaloniaManager.UIInitSource.TrySetResult();
                avaloniaManager.BuildAvaloniaApp()
                               .StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                // Disposable dependencies
                IFirewall firewall = _host.Services.GetRequiredService<IFirewall>();
                ILoggerProvider loggerProvider = _host.Services.GetRequiredService<ILoggerProvider>();

                // Shutdown dependencies
                IConsoleExtensions consoleExtensions = _host.Services.GetRequiredService<IConsoleExtensions>();
                ConnectionConfig connectionConfig = _host.Services.GetRequiredService<ConnectionConfig>();

                await _host.StopAsync().ConfigureAwait(false);

                // If your firewall is capable of offloading data from RAM to disk, for example
                if (firewall is IDisposable disposableFirewall)
                {
                    disposableFirewall.Dispose();
                }

                // [!] I strongly recommend providing the provider with disk-based logging functionality
                // that is invoked via the resource cleanup method
                if (loggerProvider is IDisposable disposableLogger)
                {
                    disposableLogger.Dispose();
                }

                if (!connectionConfig.EnableForceShutdown)
                {
                    _bootLogger.LogInformation("To finish this process, press any key...");
                    Console.ReadKey(true);
                }
            }
        }
    }
}
