using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMF.Core.Interfaces.Network;
using RMF.Core.Loaders;
using RMF.Core.Packets;
using RMF_Server.Channels;
using RMF_Server.Commands;
using RMF_Server.Configurations;
using RMF_Server.Debugger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.DI
{
    internal class RmfServerHost
    {
        private readonly IHost _host;

        private const int PadLength = -22;

        public RmfServerHost(string[]? args = null)
        {
            IHostBuilder builder = Host.CreateDefaultBuilder(args);

            builder.ConfigureServices(services =>
            {
                // Logger
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.Services.AddSingleton<ILoggerProvider, RmfLoggerProvider>();
                });

                // Configurations
                services.AddSingleton<XmlConfigLoader>(provider => new XmlConfigLoader(Path.Combine("Resources", "config.xml")));
                services.AddSingletonXmlConfig<AppearanceConfig>();
                services.AddSingletonXmlConfig<ConnectionConfig>();
                services.AddSingletonXmlConfig<FirewallConfig>();
                services.AddSingletonXmlConfig<TlsConfig>();
                services.AddSingletonXmlConfig<ControllerConfig>();
                services.AddSingletonXmlConfig<ChannelConfig>();
                services.AddSingletonXmlConfig<StreamingConfig>();
                services.AddSingletonXmlConfig<CommandConfig>();
                services.AddSingletonXmlConfig<ListenerConfig>();
                services.AddSingletonXmlConfig<LoggingConfig>();

                // Theme
                services.AddSingleton<IThemeManager, ThemeManager>(provider =>
                {
                    ILogger<ThemeManager> logger = provider.GetRequiredService<ILogger<ThemeManager>>();

                    (Dictionary<string, ThemeColor> colorsLoaded, int totalColors) = XmlThemeLoader.Load(Path.Combine("Resources", "theme.xml"), logger);
                    ThemeManager themeManager = new(colorsLoaded, new ThemeColor(255, 255, 255, 255));

                    logger.LogInformation("{Label, PadLength}: {Loaded} / {Total}", "Theme colors", colorsLoaded.Count, totalColors);
                    return themeManager;
                });

                // Commands
                services.AddSingleton<ICommandManager, CommandManager>(provider =>
                {
                    ILogger<CommandManager> logger = provider.GetRequiredService<ILogger<CommandManager>>();

                    (List<Command> commandsLoaded, int totalCommands) = XmlCommandLoader.Load(Path.Combine("Resources", "commands.xml"), logger);
                    CommandManager commandManager = new(commandsLoaded);

                    logger.LogInformation("{Label, PadLength}: {Loaded} / {Total}", "Inline commands", commandsLoaded.Count, totalCommands);
                    return commandManager;
                });

                // Packets
                services.AddSingleton<IPacketFactory, PacketFactory>(provider =>
                {
                    ILogger<PacketFactory> logger = provider.GetRequiredService<ILogger<PacketFactory>>();

                    (Dictionary<short, Type> packetsLoaded, int totalPackets) = ReflectionPacketLoader.Load(logger);
                    PacketFactory packetFactory = new(packetsLoaded);

                    logger.LogInformation("{Label, PadLength}: {Loaded} / {Total}", "Network packets", packetsLoaded.Count, totalPackets);
                    return packetFactory;
                });

                // Channels
                services.AddSingleton<IChannelDispatcher, ChannelDispatcher>(provider =>
                {
                    IPacketFactory packetFactory = provider.GetRequiredService<IPacketFactory>();
                    IServerPacketProcessor packetProcessor = provider.GetRequiredService<IServerPacketProcessor>();
                    ILogger<ChannelDispatcher> logger = provider.GetRequiredService<ILogger<ChannelDispatcher>>();
                    ChannelConfig channelConfig = provider.GetRequiredService<ChannelConfig>();

                    ChannelDispatcher channelDispatcher = new(packetFactory, packetProcessor, logger, channelConfig);
                    (int channelsLoaded, int totalChannels) = channelDispatcher.StartFound();

                    logger.LogInformation("{Label, PadLength}: {Loaded} / {Total}", "Process channels", channelsLoaded, totalChannels);
                    return channelDispatcher;
                });
            });

            this._host = builder.Build();
        }

        public async Task RunAsync()
        {
            await _host.StartAsync();
        }
    }
}
