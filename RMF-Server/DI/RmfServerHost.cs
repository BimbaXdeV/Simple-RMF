using Avalonia;
using Avalonia.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMF.Core.Events;
using RMF.Core.Interfaces.Logic;
using RMF.Core.Interfaces.Network;
using RMF.Core.Loaders;
using RMF.Core.Network;
using RMF.Core.Packets;
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

        public RmfServerHost(string[] args)
        {
            // Logging dependencies
            LoadResult<Dictionary<Type, object>> configLoadResult = XmlConfigLoader.Load(Path.Combine("Resources", "config.xml"));
            if (!configLoadResult.IsSuccess)
            {
                Console.WriteLine($"[FATAL] {configLoadResult.ExceptionMessage}");
                Environment.Exit(1);
            }
            XmlConfigProvider configProvider = new(configLoadResult.Data!);
            LoggingConfig loggingConfig = configProvider.GetConfig<LoggingConfig>();

            // Console theme
            LoadResult< Dictionary<string, ThemeColor>> themeLoadResult = XmlThemeLoader.Load(Path.Combine("Resources", "theme.xml"));
            if (!themeLoadResult.IsSuccess)
            {
                Console.WriteLine($"[FATAL] {themeLoadResult.ExceptionMessage}");
            }
            ThemeManager themeManager = new(themeLoadResult.Data!, new ThemeColor(255, 255, 255, 255));

            IHostBuilder builder = Host.CreateDefaultBuilder(args);

            builder.ConfigureServices(services =>
            {
                // Logging dependencies (color theme + synchronizer + config)
                services.AddSingleton(provider =>
                {
                    ILogger<ThemeManager> logger = provider.GetRequiredService<ILogger<ThemeManager>>();
                    logger.LogInformation(RmfConstants.InitComponentLogTemplate, "Theme colors", themeLoadResult.Data!.Count, themeLoadResult.Total);
                    return themeManager;
                });
                services.AddSingleton<IConsoleSynchronizer, ConsoleSynchronizer>();
                services.AddSingleton(loggingConfig);

                // Logging
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.Services.AddSingleton<ILoggerProvider, RmfLoggerProvider>();
                });

                // Configurations
                services.AddSingleton(provider =>
                {
                    ILogger<XmlConfigProvider> logger = provider.GetRequiredService<ILogger<XmlConfigProvider>>();
                    logger.LogInformation(RmfConstants.InitComponentLogTemplate, "Configurations", configLoadResult.Data!.Count, configLoadResult.Total);
                    return configProvider;
                });
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
                services.AddSingleton<IProtocolReader, ProtocolReader>();
                services.AddSingleton<IPacketSender, StreamManager>();
                services.AddSingleton<IServerSessionManager, SessionManager>();

                // UI
                services.AddSingleton<IAvaloniaManager, AvaloniaManager>();
                services.AddSingleton<IWindowManager, AppearanceManager>();

                // Commands
                services.AddSingleton<ICommandManager, CommandManager>(provider =>
                {
                    ILogger<CommandManager> logger = provider.GetRequiredService<ILogger<CommandManager>>();

                    LoadResult<List<Command>> commandLoadResult = XmlCommandLoader.Load(Path.Combine("Resources", "commands.xml"));
                    CommandManager commandManager = new(commandLoadResult.Data);

                    logger.LogInformation(RmfConstants.InitComponentLogTemplate, "Inline commands", commandLoadResult.Data?.Count ?? 0, commandLoadResult.Total);
                    return commandManager;
                });
                services.AddSingleton<ICommandHandler, CommandHandler>();

                // Packets
                services.AddSingleton<IPacketFactory, PacketFactory>(provider =>
                {
                    ILogger<PacketFactory> logger = provider.GetRequiredService<ILogger<PacketFactory>>();

                    (Dictionary<short, Type> packetsLoaded, int totalPackets) = ReflectionPacketLoader.Load(logger);
                    PacketFactory packetFactory = new(packetsLoaded);

                    logger.LogInformation(RmfConstants.InitComponentLogTemplate, "Network packets", packetsLoaded.Count, totalPackets);
                    return packetFactory;
                });

                // Events
                services.AddSingleton<IEventFactory, EventFactory>(provider =>
                {
                    ILogger<EventFactory> logger = provider.GetRequiredService<ILogger<EventFactory>>();

                    (Dictionary<string, Type> eventsLoaded, int totalEvents) = ReflectionEventLoader.FindEvents("Server");
                    EventFactory eventFactory = new(eventsLoaded);
                    
                    logger.LogInformation(RmfConstants.InitComponentLogTemplate, "Server events", eventsLoaded.Count, totalEvents);
                    return eventFactory;
                });

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
                services.AddSingleton<IFirewall, Firewall>(provider =>
                {
                    ILogger<Firewall> logger = provider.GetRequiredService<ILogger<Firewall>>();
                    FirewallConfig firewallConfig = provider.GetRequiredService<FirewallConfig>();
                    
                    Firewall firewall = new(logger, firewallConfig);
                    if (firewall.TryLoadBlacklist())
                    {
                        int blacklistLength = firewall.GetBannedIPsCount();
                        logger.LogInformation("Firewall connection blacklist loaded successfully ({Total} IPs)", blacklistLength);
                    }
                    return firewall;
                });
                services.AddHostedService<NetworkEngine>();
                services.AddHostedService<InputListener>();
            });

            this._host = builder.Build();
        }

        public async Task RunAsync(string[] args)
        {
            IAvaloniaManager avaloniaManager = this._host.Services.GetRequiredService<IAvaloniaManager>();

            await this._host.RunAsync();

            try
            {
                await avaloniaManager.WaitForUIReady();
                avaloniaManager.BuildAvaloniaApp()
                               .StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                await this._host.StopAsync();
            }
        }
    }
}
