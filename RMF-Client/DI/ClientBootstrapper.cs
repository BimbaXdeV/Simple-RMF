using Microsoft.Extensions.Hosting;
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
        private readonly IMonitoringFactory _monitoringFactory;
        private readonly IToolbarManager _toolbarManager;
        private readonly IWindowEffects _windowEffects;

        public ClientBootstrapper(
            IMonitoringFactory monitoringFactory,
            IToolbarManager toolbarManager,
            IWindowEffects windowEffects
        )
        {
            this._monitoringFactory = monitoringFactory;
            this._toolbarManager = toolbarManager;
            this._windowEffects = windowEffects;
        }

        public Task StartAsync(CancellationToken token)
        {
            this._windowEffects.DisplayLogo();

            IHardwareMonitor? monitor = this._monitoringFactory.GetActualMonitor(updateIfNullable: true);
            if (monitor != null)
            {
                double ramCapacityGb = monitor.RAMCapacity() / 1024.0 / 1024.0 / 1024.0;
                double vramCapacityGb = monitor.VRAMCapacity() / 1024.0 / 1024.0 / 1024.0;

                this._toolbarManager.ReplaceToolbarContent(new Dictionary<string, string>
                {
                    { "endpointMachine", monitor.MachineName() },
                    { "endpointUsername", monitor.Username() },
                    { "endpointOS", monitor.OSName() },
                    { "endpointArchitecture", $"({monitor.CPUArchitecture()}) {monitor.CPUName()}" },
                    { "endpointVideoprovider", monitor.GPUName() },
                    { "endpointMemory", "RAM: " + Math.Round(ramCapacityGb, 2) + " GB, VRAM: " + Math.Round(vramCapacityGb, 2) + " GB" },
                });
            }
            else
            {
                this._toolbarManager.DisplayToolbar();
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token) => Task.CompletedTask;
    }
}
