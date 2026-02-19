using RMF.Core.Packets;
using RMF_Client.Logic;
using RMF_Client.Network;

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

            AppearanceManager.LoadToolbar();
            AppearanceManager.ReplaceToolbarContent(new Dictionary<string, string>
            {
                { "endpointMachine", HardwareAnalyser.GetMachineName() },
                { "endpointUsername", HardwareAnalyser.GetUsername() },
                { "endpointOS", HardwareAnalyser.GetOS() },
                { "endpointArch", HardwareAnalyser.GetArchitecture() },
                { "configurationsLoaded", configurationsLoaded + " / " + totalConfigurations },
                { "packetsLoaded", packetsLoaded + " / " + totalPackets }
            });

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
