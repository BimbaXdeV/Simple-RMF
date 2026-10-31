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
using System.Linq.Expressions;
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
            // ---- Part of the server component initialization ----
            // All necessary components must be initialized before the DI container is built,
            // which should save resources during server startup;

            // [!] Ultimately, the decision was made to completely abandon partial resource loading
            // and instead throw an exception immediately if external files or parsing systems failed

            // ---- Assembling services into a DI container ----
            IHostBuilder builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                // Configurations
                services.AddSingleton(provider =>
                {
                    Dictionary<string, string> configDict = XmlConfigLoader.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "config.xml"));
                    return new RmfConfigProvider(configDict);
                });
                services.AddSingleton<IConfigViewer>(provider => provider.GetRequiredService<RmfConfigProvider>());
                services.AddSingletonConfigurations();

                // Color theme
                services.AddSingleton<IThemeManager, ThemeManager>(provider =>
                {
                    Dictionary<string, ThemeColor> serverTheme = XmlThemeLoader.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "theme.xml"));
                    ThemeColor defaultColor = new(255, 255, 255);
                    return new ThemeManager(serverTheme, defaultColor);
                });

                // Logging
                services.AddSingleton<IConsoleSynchronizer, RmfConsoleSynchronizer>();
                services.AddSingleton<IConsoleExtensions, RmfConsoleAppearance>();
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                });
                services.AddSingleton<RmfLoggerProvider>();
                services.AddSingleton<ILoggerProvider>(provider => provider.GetRequiredService<RmfLoggerProvider>());
                services.AddHostedService(provider => provider.GetRequiredService<RmfLoggerProvider>());

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

            ILoggerFactory loggerFactory = _host.Services.GetRequiredService<ILoggerFactory>();
            ILogger bootLogger = loggerFactory.CreateLogger("RmfServerBoot");

            lifetime.ApplicationStopping.Register(() =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        desktopLifetime.Shutdown();
                        bootLogger.LogInformation("Avalonia graphics thread has been successfully stopped");
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
                    bootLogger.LogInformation("To finish this process, press any key...");
                    Console.ReadKey(true);
                }
            }
        }
    }
}
