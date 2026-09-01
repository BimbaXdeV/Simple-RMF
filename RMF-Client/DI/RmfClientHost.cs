using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMF.Core.Appearance;
using RMF.Core.Events;
using RMF.Core.Extensions;
using RMF.Core.Loaders;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF_Client.Appearance;
using RMF_Client.Capture;
using RMF_Client.Configurations;
using RMF_Client.Logic;
using RMF_Client.Monitors;
using RMF_Client.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.DI
{
    internal class RmfClientHost
    {
        private readonly IHost _host;

        public RmfClientHost(string[] args)
        {
            // ---- Part of the client component initialization ----
            // All necessary components must be initialized before the DI container is built,
            // which should save resources during client startup;

            // [!] Ultimately, the decision was made to completely abandon partial resource loading
            // and instead throw an exception immediately if external files or parsing systems failed

            // Configurations
            LoadResult<Dictionary<Type, object>> configLoadResult = XmlConfigLoader.Load(Path.Combine("Resources", "config.xml"));
            if (!configLoadResult.IsSuccess)
            {
                throw new FileLoadException(configLoadResult.ExceptionMessage);
            }
            XmlConfigProvider configProvider = new(configLoadResult.Data!);

            // Toolbar content
            LoadResult<ToolbarItem[]> toolbarLoadResult = XmlToolbarLoader.Load(Path.Combine("Resources", "toolbar.xml"));
            if (!toolbarLoadResult.IsSuccess)
            {
                throw new FileLoadException(toolbarLoadResult.ExceptionMessage);
            }

            // Network packets
            LoadResult<Dictionary<short, Type>> packetLoadResult = ReflectionPacketLoader.Load();
            if (!packetLoadResult.IsSuccess)
            {
                throw new TypeLoadException(packetLoadResult.ExceptionMessage);
            }
            PacketFactory packetFactory = new(packetLoadResult.Data!);

            // Server events
            LoadResult<Dictionary<string, Type>> eventLoadResult = ReflectionEventLoader.FindEvents("Client");
            if (!eventLoadResult.IsSuccess)
            {
                throw new TypeLoadException(eventLoadResult.ExceptionMessage);
            }
            EventFactory eventFactory = new(eventLoadResult.Data!);

            // ---- Assembling services into a DI container ----
            IHostBuilder builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
            });
            builder.ConfigureServices(services =>
            {
                // Configurations
                services.AddSingleton(configProvider);
                services.AddSingletonXmlConfig<AppearanceConfig>();
                services.AddSingletonXmlConfig<CaptureConfig>();
                services.AddSingletonXmlConfig<ChannelConfig>();
                services.AddSingletonXmlConfig<ConnectionConfig>();
                services.AddSingletonXmlConfig<ControllerConfig>();
                services.AddSingletonXmlConfig<SecurityConfig>();

                // Sessions & Network
                services.AddSingleton<IProtocolReader, ProtocolReader>(provider =>
                {
                    SecurityConfig securityConfig = provider.GetRequiredService<SecurityConfig>();
                    return new ProtocolReader(securityConfig.MaxPacketBufferKB * 1024);
                });
                services.AddSingleton<IPacketSender, StreamManager>();
                services.AddSingleton<IEventFactory>(eventFactory);
                services.AddSingleton<IClientSessionManager, SessionManager>();

                // UI
                services.AddSingleton(provider =>
                {
                    AppearanceConfig appearanceConfig = provider.GetRequiredService<AppearanceConfig>();

                    AppearanceManager appearanceManager = new(appearanceConfig);
                    appearanceManager.LoadToolbar(toolbarLoadResult.Data!);

                    appearanceManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "configsLoaded", configLoadResult.Loaded + " / " + configLoadResult.Total },
                        { "packetsLoaded", packetLoadResult.Loaded + " / " + packetLoadResult.Total },
                        { "eventsLoaded", eventLoadResult.Loaded + " / " + eventLoadResult.Total }
                    }, autoUpdate: false); // Your time hasn`t come yet, bro; ClientBootstrapper ​​will sort everything out himself
                    return appearanceManager;
                });
                services.AddSingleton<IWindowManager, AppearanceManager>(provider => provider.GetRequiredService<AppearanceManager>());
                services.AddSingleton<IToolbarManager, AppearanceManager>(provider => provider.GetRequiredService<AppearanceManager>());
                services.AddSingleton<IWindowEffects, AppearanceManager>(provider => provider.GetRequiredService<AppearanceManager>());

                // Capture
                services.AddSingleton<ICaptureFactory, CaptureFactory>();

                // Hardware monitoring
                services.AddSingleton<IMonitoringFactory, MonitoringFactory>();

                // Packets
                services.AddSingleton<IPacketFactory>(packetFactory);
                services.AddSingleton<IClientPacketProcessor, PacketProcessor>();

                // Client connection
                services.AddSingleton<IConnectionFactory, TcpConnectionFactory>();
                services.AddHostedService<ClientBootstrapper>();
                services.AddHostedService<EntryEngine>();
            });

            this._host = builder.Build();
        }

        public async Task RunAsync()
        {
            IToolbarManager toolbarManager = this._host.Services.GetRequiredService<IToolbarManager>();
            IWindowEffects windowEffects = this._host.Services.GetRequiredService<IWindowEffects>();
            ConnectionConfig connectionConfig = this._host.Services.GetRequiredService<ConnectionConfig>();

            await this._host.RunAsync();

            if (!connectionConfig.EnableForceShutdown)
            {
                toolbarManager.ReplaceToolbarContent(new Dictionary<string, string>
                {
                    { "endpointTime", "To finish this process, press any key..." }
                });
                Console.ReadKey(true);
            }

            await windowEffects.Curtain();
        }
    }
}
