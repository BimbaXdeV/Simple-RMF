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
            LoadResult<Dictionary<Type, object>> configLoadResult = XmlConfigLoader.Load(Path.Combine("Resources", "config.xml"));
            if (!configLoadResult.IsSuccess)
            {
                throw new FileLoadException(configLoadResult.ExceptionMessage);
            }
            XmlConfigProvider configProvider = new(configLoadResult.Data!);
            LoggingConfig loggingConfig = configProvider.GetConfig<LoggingConfig>();

            // Console theme
            LoadResult< Dictionary<string, ThemeColor>> themeLoadResult = XmlThemeLoader.Load(Path.Combine("Resources", "theme.xml"));
            if (!themeLoadResult.IsSuccess)
            {
                throw new FileLoadException(themeLoadResult.ExceptionMessage);
            }
            ThemeManager themeManager = new(themeLoadResult.Data!, new ThemeColor(255, 255, 255));

            // Network packets
            LoadResult<Dictionary<short, Type>> packetLoadResult = ReflectionPacketLoader.Load();
            if (!packetLoadResult.IsSuccess)
            {
                throw new TypeLoadException(packetLoadResult.ExceptionMessage);
            }
            PacketFactory packetFactory = new(packetLoadResult.Data!);

            // Server events
            LoadResult<Dictionary<string, Type>> eventLoadResult = ReflectionEventLoader.FindEvents("Server");
            if (!eventLoadResult.IsSuccess)
            {
                throw new TypeLoadException(eventLoadResult.ExceptionMessage);
            }
            EventFactory eventFactory = new(eventLoadResult.Data!);

            // Admin commands
            LoadResult<List<Command>> commandLoadResult = XmlCommandLoader.Load(Path.Combine("Resources", "commands.xml"));
            if (!commandLoadResult.IsSuccess)
            {
                throw new FileLoadException(commandLoadResult.ExceptionMessage);
            }
            CommandManager commandManager = new(commandLoadResult.Data);

            // ---- Registering the logger provider and initial console output ----
            ConsoleSynchronizer consoleSynchronizer = new();
            RmfLoggerProvider loggerProvider = new(themeManager, consoleSynchronizer, loggingConfig);
            RmfConsoleAppearance consoleAppearance = new(themeManager, loggingConfig);
            this._bootLogger = loggerProvider.CreateLogger("RmfServerBoot");

            consoleAppearance.DrawLogo(this._bootLogger);
            consoleAppearance.LogSeparator(this._bootLogger);

            this._bootLogger.LogInformation("Initialized components:");
            consoleAppearance.LogInitialization(this._bootLogger, "Configurations", configLoadResult.Loaded, configLoadResult.Total);
            consoleAppearance.LogInitialization(this._bootLogger, "Theme colors", themeLoadResult.Loaded, themeLoadResult.Total);
            consoleAppearance.LogInitialization(this._bootLogger, "Network packets", packetLoadResult.Loaded, packetLoadResult.Total);
            consoleAppearance.LogInitialization(this._bootLogger, "Server events", eventLoadResult.Loaded, eventLoadResult.Total);
            consoleAppearance.LogInitialization(this._bootLogger, "Admin commands", commandLoadResult.Loaded, commandLoadResult.Total);
            consoleAppearance.LogSeparator(this._bootLogger);
            this._bootLogger.LogInformation("Preparing to launch the server:");

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
                services.AddSingleton(configProvider);
                services.AddSingletonXmlConfig<AppearanceConfig>();
                services.AddSingletonXmlConfig<ConnectionConfig>();
                services.AddSingletonXmlConfig<FirewallConfig>();
                services.AddSingletonXmlConfig<TlsConfig>();
                services.AddSingletonXmlConfig<ControllerConfig>();
                services.AddSingletonXmlConfig<ChannelConfig>();
                services.AddSingletonXmlConfig<StreamingConfig>();
                services.AddSingletonXmlConfig<CommandConfig>();
                services.AddSingletonXmlConfig<ListenerConfig>();

                // Sessions & Network
                services.AddSingleton<IProtocolReader, ProtocolReader>(provider =>
                {
                    FirewallConfig firewallConfig = provider.GetRequiredService<FirewallConfig>();
                    return new ProtocolReader(firewallConfig.MaxPacketBufferKB * 1024);
                });
                services.AddSingleton<IPacketSender, StreamManager>();
                services.AddSingleton<IEventFactory>(eventFactory);
                services.AddSingleton<IServerSessionManager, SessionManager>();

                // UI
                services.AddSingleton<IAvaloniaManager, AvaloniaManager>();
                services.AddSingleton<IWindowManager, AppearanceManager>();

                // Commands
                services.AddSingleton<ICommandManager>(commandManager);
                services.AddSingleton<ICommandHandler, CommandHandler>();

                // Packets
                services.AddSingleton<IPacketFactory>(packetFactory);
                services.AddSingleton<IServerPacketProcessor, PacketProcessor>();

                // Channels
                services.AddSingleton<IChannelDispatcher, ChannelDispatcher>();
                services.AddHostedService(provider => (ChannelDispatcher)provider.GetRequiredService<IChannelDispatcher>());

                // Server
                services.AddSingleton<IConnectionListener, TcpListenerAdapter>(provider =>
                {
                    ConnectionConfig connectionConfig = provider.GetRequiredService<ConnectionConfig>();

                    IPAddress ip = connectionConfig.IPAddress != "Any"
                        ? IPAddress.Parse(connectionConfig.IPAddress ?? "127.0.0.1")
                        : IPAddress.Any;

                    int port = (connectionConfig.Port >= IPEndPoint.MinPort && connectionConfig.Port <= IPEndPoint.MaxPort)
                        ? connectionConfig.Port
                        : 8000;  // Default port if the provided port is invalid

                    TcpListener listener = new(ip, port);
                    return new TcpListenerAdapter(listener);
                });
                services.AddSingleton<ITlsManager, TlsManager>();
                services.AddSingleton<IFirewall, Firewall>();
                services.AddHostedService<NetworkEngine>();
                services.AddHostedService<InputListener>();
            });

            this._host = builder.Build();
        }

        [STAThread]
        public async Task RunAsync(string[] args)
        {
            IHostApplicationLifetime lifetime = this._host.Services.GetRequiredService<IHostApplicationLifetime>();
            IAvaloniaManager avaloniaManager = this._host.Services.GetRequiredService<IAvaloniaManager>();

            lifetime.ApplicationStopping.Register(() =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        desktopLifetime.Shutdown();
                        this._bootLogger.LogInformation("Avalonia graphics thread has been successfully stopped");
                    });
                }
            });

            // It`s not that this service is strictly necessary here, but currently nothing uses it as a dependency;
            // However, without that line, its constructor, which handles the online status binding, simply won`t execute
            this._host.Services.GetRequiredService<IWindowManager>();

            await this._host.StartAsync();

            try
            {
                avaloniaManager.UIInitSource.TrySetResult();
                avaloniaManager.BuildAvaloniaApp()
                               .StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                IConsoleExtensions consoleExtensions = this._host.Services.GetRequiredService<IConsoleExtensions>();
                ConnectionConfig connectionConfig = this._host.Services.GetRequiredService<ConnectionConfig>();

                await this._host.StopAsync().ConfigureAwait(false);

                if (!connectionConfig.EnableForceShutdown)
                {
                    consoleExtensions.LogSeparator(this._bootLogger);
                    this._bootLogger.LogInformation("To finish this process, press any key...");
                    Console.ReadKey(true);
                }
            }
        }
    }
}
