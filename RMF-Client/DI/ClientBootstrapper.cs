using Microsoft.Extensions.Hosting;
using RMF.Core.Events;
using RMF.Core.Loaders;
using RMF.Core.Packets;
using RMF_Client.Appearance;
using RMF_Client.Monitors;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.DI
{
    internal class ClientBootstrapper : IHostedService
    {
        private readonly IConfigViewer _configViewer;
        private readonly IEventFactory _eventFactory;
        private readonly IPacketFactory _packetFactory;
        private readonly IMonitoringFactory _monitoringFactory;
        private readonly IToolbarManager _toolbarManager;
        private readonly IWindowEffects _windowEffects;

        public ClientBootstrapper(
            IConfigViewer configViewer,
            IEventFactory eventFactory,
            IPacketFactory packetFactory,
            IMonitoringFactory monitoringFactory,
            IToolbarManager toolbarManager,
            IWindowEffects windowEffects
        )
        {
            _configViewer = configViewer;
            _eventFactory = eventFactory;
            _packetFactory = packetFactory;
            _monitoringFactory = monitoringFactory;
            _toolbarManager = toolbarManager;
            _windowEffects = windowEffects;
        }

        public Task StartAsync(CancellationToken token)
        {
            _windowEffects.DisplayLogo();

            Dictionary<string, string> toolbarContent = new()
            {
                { "configsLoaded", _configViewer.RegisteredFieldsCount + " (" + _configViewer.RegisteredCategoriesCount + " categories)" },
                { "packetsLoaded", _packetFactory.RegisteredCount.ToString() },
                { "eventsLoaded", _eventFactory.RegisteredCount.ToString() }
            };

            IHardwareMonitor? monitor = _monitoringFactory.GetActualMonitor(updateIfNullable: true);
            if (monitor != null)
            {
                double ramCapacityGb = monitor.RAMCapacity() / 1024.0 / 1024.0 / 1024.0;
                double vramCapacityGb = monitor.VRAMCapacity() / 1024.0 / 1024.0 / 1024.0;

                toolbarContent.Add("endpointMachine", monitor.MachineName());
                toolbarContent.Add("endpointUsername", monitor.Username());
                toolbarContent.Add("endpointOS", monitor.OSName());
                toolbarContent.Add("endpointArchitecture", $"({monitor.CPUArchitecture()}) {monitor.CPUName()}");
                toolbarContent.Add("endpointVideoprovider", monitor.GPUName());
                toolbarContent.Add("endpointMemory", "RAM: " + Math.Round(ramCapacityGb, 2) + " GB, VRAM: " + Math.Round(vramCapacityGb, 2) + " GB");
            }

            _toolbarManager.ReplaceToolbarContent(toolbarContent);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token) => Task.CompletedTask;
    }
}
