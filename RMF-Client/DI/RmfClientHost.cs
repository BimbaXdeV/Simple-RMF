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
            // Configurations
            LoadResult<Dictionary<Type, object>> configLoadResult = XmlConfigLoader.Load(Path.Combine("Resources", "config.xml"));
            if (!configLoadResult.IsSuccess)
            {
                throw new FileLoadException(configLoadResult.ExceptionMessage);
            }
            XmlConfigProvider configProvider = new(configLoadResult.Data!);

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
                    return new ProtocolReader(securityConfig.MinPacketBufferKB, securityConfig.MaxPacketBufferKB);
                });
                services.AddSingleton<IPacketSender, StreamManager>();
                services.AddSingleton<IEventFactory>(eventFactory);
                services.AddSingleton<IClientSessionManager, SessionManager>();

                // UI
                services.AddSingleton<IWindowManager, AppearanceManager>();
                services.AddSingleton<IToolbarManager, AppearanceManager>();

                // Capture
                services.AddSingleton<ICaptureFactory, CaptureFactory>();

                // Hardware monitoring
                services.AddSingleton<IMonitoringFactory, MonitoringFactory>();

                // Packets
                services.AddSingleton<IPacketFactory>(packetFactory);
                services.AddSingleton<IClientPacketProcessor, PacketProcessor>();

                // Client connection
                services.AddSingleton<IConnectionFactory, TcpConnectionFactory>();
                services.AddHostedService<EntryEngine>();
            });

            this._host = builder.Build();
        }

        public async Task RunAsync()
        {
            await this._host.RunAsync();
        }
    }
}
