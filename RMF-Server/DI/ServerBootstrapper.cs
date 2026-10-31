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
        private readonly IConsoleExtensions _consoleExtensions;
        private readonly ILogger<ServerBootstrapper> _logger;

        public ServerBootstrapper(
            IConfigViewer configViewer,
            IEventFactory eventFactory,
            IPacketFactory packetFactory,
            IChannelDispatcher channelDispatcher,
            ICommandViewer commandViewer,
            IConsoleExtensions consoleExtensions,
            ILogger<ServerBootstrapper> logger
        )
        {
            _configViewer = configViewer;
            _eventFactory = eventFactory;
            _packetFactory = packetFactory;
            _channelDispatcher = channelDispatcher;
            _commandViewer = commandViewer;
            _consoleExtensions = consoleExtensions;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken token)
        {
            _consoleExtensions.DrawLogo(_logger);
            _logger.LogInformation("Developed by {Author} | On build version: v{Version}", RmfMeta.Author, RmfMeta.AppVersion?.ToString(3));
            _logger.LogInformation("Preparing to launch the server...");
            _consoleExtensions.LogSeparator(_logger);
            _logger.LogInformation("Server features initialized:");
            _logger.LogInformation(
                "Configurations:  {FieldsCount} ({CategoriesCount} categories)",
                _configViewer.RegisteredFieldsCount,
                _configViewer.RegisteredCategoriesCount
            );
            _logger.LogInformation("Inline commands: {Count}", _commandViewer.RegisteredCount);
            _logger.LogInformation("Channels       : {Count}", _channelDispatcher.RegisteredCount);

            _consoleExtensions.LogSeparator(_logger);
            _logger.LogInformation("Core subsystems initialized:");
            _logger.LogInformation("Network packets: {Count}", _packetFactory.RegisteredCount);
            _logger.LogInformation("Server events:   {Count}", _eventFactory.RegisteredCount);

            _consoleExtensions.LogSeparator(_logger);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token) => Task.CompletedTask;
    }
}
