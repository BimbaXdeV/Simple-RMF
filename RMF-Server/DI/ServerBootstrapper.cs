using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMF.Core.Events;
using RMF.Core.Loaders;
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

namespace RMF_Server.DI
{
    internal class ServerBootstrapper : IHostedService
    {
        private readonly IConfigViewer _configViewer;
        private readonly IEventFactory _eventFactory;
        private readonly IPacketFactory _packetFactory;
        private readonly IChannelDispatcher _channelDispatcher;
        private readonly ICommandViewer _commandViewer;
        private readonly IThemeManager _themeManager;
        private readonly ILogger<ServerBootstrapper> _logger;
        private readonly IConsoleExtensions _consoleExtensions;

        public ServerBootstrapper(
            IConfigViewer configViewer,
            IEventFactory eventFactory,
            IPacketFactory packetFactory,
            IChannelDispatcher channelDispatcher,
            ICommandViewer commandViewer,
            IThemeManager themeManager,
            ILogger<ServerBootstrapper> logger,
            IConsoleExtensions consoleExtensions
        )
        {
            _configViewer = configViewer;
            _eventFactory = eventFactory;
            _packetFactory = packetFactory;
            _channelDispatcher = channelDispatcher;
            _commandViewer = commandViewer;
            _themeManager = themeManager;
            _logger = logger;
            _consoleExtensions = consoleExtensions;
        }

        public Task StartAsync(CancellationToken token)
        {
            ThemeColor highlightColor = _themeManager.GetColor("InitHighlight");
            ThemeColor successColor = _themeManager.GetColor("SuccessfullyLoaded");
            ThemeColor secondaryColor = _themeManager.GetColor("InitSecondary");
            ThemeColor failColor = _themeManager.GetColor("FailedToLoad");

            // Server startup information
            _consoleExtensions.DrawLogo(_logger);
            _logger.LogInformation(
                "Developed by {AuthorColorStart}{Author}{AuthorColorEnd} | On build version: {VersionColorStart}v{Version}{VersionColorEnd}",
                highlightColor, RmfMeta.Author, ThemeColor.AnsiReset, highlightColor, RmfMeta.AppVersion?.ToString(3), ThemeColor.AnsiReset
            );
            _logger.LogInformation("Preparing to launch the server...");
            _consoleExtensions.LogSeparator(_logger);
            
            // Server-only features
            int registeredConfigFields = _configViewer.RegisteredFieldsCount;
            ThemeColor hlConfigColor = registeredConfigFields > 0 ? successColor : failColor;
            ThemeColor sdConfigColor = registeredConfigFields > 0 ? secondaryColor : failColor;
            int registeredCommands = _commandViewer.RegisteredCount;
            ThemeColor hlCommandColor = registeredCommands > 0 ? successColor : failColor;
            int registeredChannels = _channelDispatcher.RegisteredCount;
            ThemeColor hlChannelColor = registeredChannels > 0 ? successColor : failColor;

            _logger.LogInformation("Server features initialized:");
            _logger.LogInformation("Configurations : {InitColorStart}{Count}{InitColorEnd} {SecondaryColorStart}({CategoriesCount} categories){SecondaryColorEnd}",
                hlConfigColor, registeredConfigFields, ThemeColor.AnsiReset, sdConfigColor, _configViewer.RegisteredCategoriesCount, ThemeColor.AnsiReset
            );
            _logger.LogInformation("Inline commands: {InitColorStart}{Count}{InitColorEnd}", hlCommandColor, registeredCommands, ThemeColor.AnsiReset);
            _logger.LogInformation("Channels       : {InitColorStart}{Count}{InitColorEnd}", hlChannelColor, registeredChannels, ThemeColor.AnsiReset);
            _consoleExtensions.LogSeparator(_logger);

            // RMF.Core (shared) features
            int registeredPackets = _packetFactory.RegisteredCount;
            ThemeColor hlPacketColor = registeredPackets > 0 ? successColor : failColor;
            int registeredEvents = _eventFactory.RegisteredCount;
            ThemeColor hlEventColor = registeredEvents > 0 ? successColor : failColor;

            _logger.LogInformation("Core subsystems initialized:");
            _logger.LogInformation("Network packets: {InitColorStart}{Count}{InitColorEnd}", hlPacketColor, registeredPackets, ThemeColor.AnsiReset);
            _logger.LogInformation("Netword events:  {InitColorStart}{Count}{InitColorEnd}", hlEventColor, registeredEvents, ThemeColor.AnsiReset);

            _consoleExtensions.LogSeparator(_logger);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token) => Task.CompletedTask;
    }
}
