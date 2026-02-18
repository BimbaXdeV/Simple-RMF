using RMF.Core.Packets;
using RMF_Client.Logic;
using RMF_Client.Network;

namespace RMF_Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle} | Offline");
            AppearanceManager.DisplayLogo();

            (int configurationsLoaded, int totalConfigurations) = ConfigurationManager.Load();
            (int packetsLoaded, int totalPackets) = PacketsAssembler.RegisterFound();

            AppearanceManager.LoadToolbar();
            AppearanceManager.DisplayToolbar();

            using CancellationTokenSource cts = new();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            EntryTCP tcp = new();
            Task connectionTask = tcp.Connect(cts.Token);
        }
    }
}
