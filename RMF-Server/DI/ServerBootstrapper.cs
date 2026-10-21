using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMF.Core.Events;
using RMF.Core.Loaders;
using RMF.Core.Packets;
using RMF_Server.Commands;
using RMF_Server.Debugger;
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
        private readonly ICommandViewer _commandViewer;
        private readonly IPacketFactory _packetFactory;
        private readonly IEventFactory _eventFactory;
        private readonly IConsoleExtensions _consoleExtensions;
        private readonly ILogger<ServerBootstrapper> _logger;

        public ServerBootstrapper(
            IConfigViewer configViewer,
            ICommandViewer commandViewer,
            IPacketFactory packetFactory,
            IEventFactory eventFactory,
            IConsoleExtensions consoleExtensions,
            ILogger<ServerBootstrapper> logger
        )
        {
            _configViewer = configViewer;
            _commandViewer = commandViewer;
            _packetFactory = packetFactory;
            _eventFactory = eventFactory;
            _consoleExtensions = consoleExtensions;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken token)
        {
            _consoleExtensions.LogSeparator(_logger);
            _logger.LogInformation("Server features initialized:");
            _logger.LogInformation(
                "Configurations:  {FieldsCount} ({CategoriesCount} categories)",
                _configViewer.RegisteredFieldsCount,
                _configViewer.RegisteredCategoriesCount
            );
            _logger.LogInformation("Inline commands: {Count}", _commandViewer.RegisteredCount);

            _consoleExtensions.LogSeparator(_logger);
            _logger.LogInformation("Core subsystems initialized:");
            _logger.LogInformation("Network packets: {Count}", _packetFactory.RegisteredCount);
            _logger.LogInformation("Server events:   {Count}", _eventFactory.RegisteredCount);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token) => Task.CompletedTask;
    }
}
