using RMF.Core.Events;
using RMF.Core.Interfaces;
using RMF.Core.Packets;
using RMF_Client.Logic;
using RMF_Client.Monitors;
using RMF_Client.Network;
using RMF_Server.Logic;

namespace RMF_Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            AppearanceManager.SetTitle(ConfigurationManager.AppTitle + "| Offline");
            AppearanceManager.DisplayLogo();

            (int configurationsLoaded, int totalConfigurations) = ConfigurationManager.Load();
            (int packetsLoaded, int totalPackets) = PacketsAssembler.RegisterFound();
            (int eventsLoaded, int totalEvents) = EventAssembler.RegisterFound("Client");

            // Transferring fields data from server configurations to core packet configurations
            SettingsSynchronizer.Upload(typeof(ConfigurationManager), typeof(PacketConfigurations));

            AppearanceManager.LoadToolbar();
            IHardwareMonitor? hardwareMonitor = MonitoringFactory.GetActualMonitor(updateIfNullable: true);
            if (hardwareMonitor != null)
            {
                double ramCapacityGB = hardwareMonitor.RAMCapacity() / 1024.0 / 1024.0 / 1024.0;
                double vramCapacityGB = hardwareMonitor.VRAMCapacity() / 1024.0 / 1024.0 / 1024.0;

                AppearanceManager.ReplaceToolbarContent(new Dictionary<string, string>
                {
                    { "endpointMachine", hardwareMonitor.MachineName() },
                    { "endpointUsername", hardwareMonitor.Username() },
                    { "endpointOS", hardwareMonitor.OSName() },
                    { "endpointArchitecture", $"({hardwareMonitor.CPUArchitecture()}) {hardwareMonitor.CPUName()}" },
                    { "endpointVideoprovider", hardwareMonitor.GPUName() },
                    { "endpointMemory", "RAM: " + Math.Round(ramCapacityGB, 2) + " GB, VRAM: " + Math.Round(vramCapacityGB, 2) + " GB" },
                    { "configsLoaded", configurationsLoaded + " / " + totalConfigurations },
                    { "packetsLoaded", packetsLoaded + " / " + totalPackets },
                    { "eventsLoaded", eventsLoaded + " / " + totalEvents }
                });
            }
            else
            {
                AppearanceManager.SetTitle(ConfigurationManager.AppTitle + "| Unsupported OS");
            }
            AppearanceManager.DisplayToolbar();

            using CancellationTokenSource cts = new();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            EntryTCP tcp = new();
            await tcp.Connect(cts.Token);
            await AppearanceManager.Curtain(0.08f);
            await Task.Delay(500);
        }
    }
}
