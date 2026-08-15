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
            // -- Part of the server component initialization --
            // All necessary components must be initialized before the DI container is built,
            // which should save resources during server startup;

            // [!] Ultimately, the decision was made to completely abandon partial resource loading
            // and instead throw an exception immediately if external files or parsing systems failed

            // Logging dependencies (color theme + synchronizer + config)
            Console.WriteLine("Loading logging dependencies...");
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
            ThemeManager themeManager = new(themeLoadResult.Data!, new ThemeColor(255, 255, 255, 255));

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

            // Firewall reserve
            string? firewallLoadMessage = null;

            // -- Assembling services into a DI container --
            Console.WriteLine("Assembling services into a DI container...");
            IHostBuilder builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                // Logging dependencies implementation
                services.AddSingleton<IThemeManager>(themeManager);
                services.AddSingleton<IConsoleSynchronizer, ConsoleSynchronizer>();
                services.AddSingleton(loggingConfig);

                // Logging provider + background executor
                services.AddSingleton<RmfLoggerProvider>();
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.Services.AddSingleton<ILoggerProvider>(provider => provider.GetRequiredService<RmfLoggerProvider>());
                });
                services.AddHostedService(provider => provider.GetRequiredService<RmfLoggerProvider>());

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
                    return new ProtocolReader(firewallConfig.MinPacketLengthKB, firewallConfig.MaxPacketLengthKB);
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
                services.AddSingleton<IFirewall, Firewall>(provider =>
                {
                    ILogger<Firewall> logger = provider.GetRequiredService<ILogger<Firewall>>();
                    FirewallConfig firewallConfig = provider.GetRequiredService<FirewallConfig>();
                    
                    Firewall firewall = new(logger, firewallConfig);
                    if (firewall.TryLoadBlacklist())
                    {
                        int blacklistLength = firewall.GetBannedIPsCount();
                        firewallLoadMessage = $"Firewall connection blacklist loaded successfully ({blacklistLength} IPs)";
                    }
                    else
                    {
                        
                    }
                    return firewall;
                });
                services.AddHostedService<NetworkEngine>();
                services.AddHostedService<InputListener>();
            });

            Console.WriteLine("Building DI container...");
            this._host = builder.Build();

            // -- Start outputs --
            ILogger<RmfServerHost> logger = this._host.Services.GetRequiredService<ILogger<RmfServerHost>>();

            logger.InsertLogo();
            logger.InsertSeparator();

            logger.LogInformation("Initialized components");
            logger.LogInformation(RmfConstants.InitComponentLogTemplate, "Configurations", configLoadResult.Data!.Count, configLoadResult.Total);
            logger.LogInformation(RmfConstants.InitComponentLogTemplate, "Theme colors", themeLoadResult.Data!.Count, themeLoadResult.Total);
            logger.LogInformation(RmfConstants.InitComponentLogTemplate, "Network packets", packetLoadResult.Data!.Count, packetLoadResult.Total);
            logger.LogInformation(RmfConstants.InitComponentLogTemplate, "Server events", eventLoadResult.Data!.Count, eventLoadResult.Total);
            logger.LogInformation(RmfConstants.InitComponentLogTemplate, "Admin commands", commandLoadResult.Data!.Count, commandLoadResult.Total);
        }

        [STAThread]
        public async Task RunAsync(string[] args)
        {
            IAvaloniaManager avaloniaManager = this._host.Services.GetRequiredService<IAvaloniaManager>();

            await this._host.StartAsync();

            try
            {
                Console.WriteLine("Starting Avalonia UI...");
                avaloniaManager.UIInitSource.TrySetResult();
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
