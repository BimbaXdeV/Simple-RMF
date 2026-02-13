using RMF.Core.Packets;
using RMF_Client.Logic;

namespace RMF_Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppearanceManager.SetTitle($"{ConfigurationManager.AppTitle} | Offline");

            (int configurationsLoaded, int totalConfigurations) = ConfigurationManager.Load();
            (int packetsLoaded, int totalPackets) = PacketsAssembler.RegisterFound();
        }
    }
}
